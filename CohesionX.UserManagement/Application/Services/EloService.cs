using AutoMapper;
using CohesionX.UserManagement.Application.Interfaces;
using CohesionX.UserManagement.Domain.Entities;
using CohesionX.UserManagement.Persistence.Interfaces;
using SharedLibrary.AppEnums;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Application.Services;

public class EloService : IEloService
{
	private readonly IEloRepository _repo;
	private readonly IUserRepository _userRepo;
	private readonly IWorkflowEngineClient _workflowEngineClient;
	private readonly IUserStatisticsRepository _userStatRepo;
	private readonly IMapper _mapper;
	private readonly IUnitOfWork _unitOfWork;
	private readonly int _eloKFactorNew;
	private readonly int _eloKFactorEstablished;
	private readonly int _eloKFactorExpert;

	public EloService(IEloRepository repo
		, IUserRepository userRepo
		, IUserStatisticsRepository userStatRepo
		, IUnitOfWork unitOfWork
		, IMapper mapper
		, IConfiguration config
		, IWorkflowEngineClient workflowEngineClient)
	{
		_repo = repo;
		_userRepo = userRepo;
		_unitOfWork = unitOfWork;
		_userStatRepo = userStatRepo;
		_mapper = mapper;
		_eloKFactorNew = int.TryParse(config["ELO_K_FACTOR_NEW"], out int parsedValue) ? parsedValue : 32;
		_eloKFactorEstablished = int.TryParse(config["ELO_K_FACTOR_ESTABLISHED"], out int parsedValue2) ? parsedValue2 : 24;
		_eloKFactorExpert = int.TryParse(config["ELO_K_FACTOR_EXPERT"], out int parsedValue3) ? parsedValue3 : 16;
		_workflowEngineClient = workflowEngineClient;
	}

	public async Task<EloUpdateResponse> ApplyEloUpdatesAsync(EloUpdateRequest request)
	{
		var eloUpdateResp = new EloUpdateResponse
		{
			WorkflowRequestId = request.WorkflowRequestId,
			ComparisonId = request.QaComparisonId,
			UpdatedAt = DateTime.UtcNow
		};

		var userIds = request.RecommendedEloChanges
			.Select(r => r.TranscriberId)
			.Distinct()
			.ToList();

		var userStatisticsDb = await _unitOfWork.UserStatistics
			.GetByUserIdsAsync(userIds, trackChanges: true);

		if (userStatisticsDb == null || userStatisticsDb.Count != userIds.Count)
			throw new Exception("Missing UserStatistics for some transcribers.");

		var eloHistoryRecords = new List<EloHistory>();

		foreach (var eloChange in request.RecommendedEloChanges)
		{
			var userStats = userStatisticsDb
				.FirstOrDefault(us => us.UserId == eloChange.TranscriberId)
				?? throw new Exception($"User statistics not found for user {eloChange.TranscriberId}");

			// Validate current elo matches OldElo
			if (userStats.CurrentElo != eloChange.OldElo)
			{
				throw new Exception($"Current Elo mismatch for user {eloChange.TranscriberId}. Expected {userStats.CurrentElo}, but request has {eloChange.OldElo}.");
			}

			var newElo = eloChange.OldElo + eloChange.RecommendedChange;

			var eloHistoryRecord = new EloHistory
			{
				UserId = eloChange.TranscriberId,
				OldElo = eloChange.OldElo,
				NewElo = newElo,
				OpponentElo = eloChange.OpponentElo,
				Reason = request.ComparisonMetadata.QaMethod ?? "",
				ComparisonId = request.QaComparisonId,
				JobId = request.WorkflowRequestId ?? "",
				Outcome = eloChange.ComparisonOutcome ?? "",
				ComparisonType = request.ComparisonMetadata.ComparisonType ?? "",
				KFactorUsed = CalculateKFactor(userStats.GamesPlayed),
				ChangedAt = DateTime.UtcNow
			};

			// Update stats in-memory
			userStats.CurrentElo = newElo;
			userStats.PeakElo = Math.Max(userStats.PeakElo, newElo);
			userStats.GamesPlayed++;
			userStats.LastCalculated = eloHistoryRecord.ChangedAt;
			userStats.UpdatedAt = DateTime.UtcNow;

			eloHistoryRecords.Add(eloHistoryRecord);

			eloUpdateResp.EloUpdatesApplied.Add(new EloUpdateAppliedDto
			{
				TranscriberId = eloChange.TranscriberId,
				OldElo = eloChange.OldElo,
				NewElo = newElo,
				EloChange = eloChange.RecommendedChange,
				ComparisonOutcome = eloChange.ComparisonOutcome
			});
		}
		_unitOfWork.UserStatistics.UpdateRange(userStatisticsDb);
		await _unitOfWork.EloHistories.AddRangeAsync(eloHistoryRecords);
		await _unitOfWork.SaveChangesAsync();
		var notifyEloUpdateReq = new EloUpdateNotificationRequest
		{
			UpdateId = request.WorkflowRequestId!,
			EventType = WorkflowEventType.EloUpdated.ToDisplayName(),
			EventData = new EloUpdateEventData
			{
				ComparisonId = request.QaComparisonId!,
				UsersUpdated = eloUpdateResp.EloUpdatesApplied.Count,
				UpdateResults = eloUpdateResp.EloUpdatesApplied.Select(u => new EloUpdateResult
				{
					UserId = u.TranscriberId,
					NewElo = u.NewElo,
					Change = u.EloChange
				}).ToList()
			}
		};

		await _workflowEngineClient.NotifyEloUpdatedAsync(notifyEloUpdateReq);
		return eloUpdateResp;
	}

	public async Task<EloHistoryResponse> GetEloHistoryAsync(Guid userId)
	{
		var user = await _userRepo.GetUserByIdAsync(userId, includeRelated: true);
		if (user == null) throw new KeyNotFoundException("User not found");

		var stats = user.Statistics;
		var eloHistories = user.EloHistories.OrderBy(eh => eh.ChangedAt).ToList();
		var currentElo = stats?.CurrentElo ?? 0;
		var initialElo = eloHistories.FirstOrDefault()?.OldElo ?? 1200;
		var peakElo = stats?.PeakElo ?? currentElo;
		var gamesPlayed = stats?.GamesPlayed ?? eloHistories.Count;

		var eloTrend7 = GetEloTrend(eloHistories, 7);
		var eloTrend30 = GetEloTrend(eloHistories, 30);
		var winRate = GetWinRate(eloHistories);
		var avgOpponentElo = GetAverageOpponentElo(eloHistories);

		var historyList = eloHistories.Select(eh => new EloEntryDto
		{
			Date = eh.ChangedAt,
			OldElo = eh.OldElo,
			NewElo = eh.NewElo,
			Outcome = eh.Outcome, // e.g. "win", "loss"
			JobId = eh.JobId
		}).ToList();

		var response = new EloHistoryResponse
		{
			UserId = user.Id,
			CurrentElo = currentElo,
			PeakElo = peakElo,
			InitialElo = initialElo,
			GamesPlayed = gamesPlayed,
			EloHistory = historyList,
			Trends = new EloTrendsDto
			{
				Last7Days = eloTrend7,
				Last30Days = eloTrend30,
				WinRate = winRate,
				AverageOpponentElo = avgOpponentElo
			}
		};

		return response;
	}

	public async Task<string> GetEloTrend(Guid userId, int days)
	{
		var cutOffDate = DateTime.UtcNow.AddDays(-days);

		var recentHistory = await _repo.FindAsync(
			eh => eh.UserId == userId && eh.ChangedAt >= cutOffDate
		);

		if (recentHistory == null || recentHistory.Count < 2)
			return $"0_over_{days}_days";

		var earliest = recentHistory.OrderBy(eh => eh.ChangedAt).First().NewElo;
		var latest = recentHistory.OrderByDescending(eh => eh.ChangedAt).First().NewElo;

		var diff = latest - earliest;
		var sign = diff >= 0 ? "+" : "-";

		return $"{sign}{Math.Abs(diff)}_over_{days}_days";
	}

	public string GetEloTrend(List<EloHistory> eloHistories, int days)
	{
		var cutOffDate = DateTime.UtcNow.AddDays(-days);

		var recentHistory = eloHistories.Where(eh => eh.ChangedAt >= cutOffDate).ToList();

		if (recentHistory == null || recentHistory.Count < 2)
			return $"0_over_{days}_days";

		var earliest = recentHistory.OrderBy(eh => eh.ChangedAt).First().OldElo;
		var latest = recentHistory.OrderByDescending(eh => eh.ChangedAt).First().NewElo;

		var diff = latest - earliest;
		var sign = diff >= 0 ? "+" : "-";

		return $"{sign}{Math.Abs(diff)}_over_{days}_days";
	}

	public async Task<Dictionary<Guid, string>> BulkEloTrendAsync(List<Guid> userIds, int days)
	{
		var cutOffDate = DateTime.UtcNow.AddDays(-days);

		var recentHistories = await _repo.FindAsync(
			eh => userIds.Contains(eh.UserId) && eh.ChangedAt >= cutOffDate
		);

		var grouped = recentHistories
			.GroupBy(eh => eh.UserId)
			.ToDictionary(
				g => g.Key,
				g =>
				{
					var ordered = g.OrderBy(e => e.ChangedAt).ToList();
					if (ordered.Count < 2)
						return $"0_over_{days}_days";

					var earliest = ordered.First().NewElo;
					var latest = ordered.Last().NewElo;
					var diff = latest - earliest;
					var sign = diff >= 0 ? "+" : "-";
					return $"{sign}{Math.Abs(diff)}_over_{days}_days";
				}
			);

		// Handle users with no history
		foreach (var userId in userIds)
		{
			if (!grouped.ContainsKey(userId))
			{
				grouped[userId] = $"0_over_{days}_days";
			}
		}

		return grouped;
	}

	private int CalculateKFactor(int gamesPlayed)
	{
		return gamesPlayed switch
		{
			< 30 => _eloKFactorNew,
			< 100 => _eloKFactorEstablished,
			_ => _eloKFactorExpert
		};
	}

	public double GetWinRate(List<EloHistory> eloHistories, int? days = null)
	{
		if (days.HasValue)
		{
			var cutOffDate = DateTime.UtcNow.AddDays(-days.Value);
			eloHistories = eloHistories.Where(eh => eh.ChangedAt >= cutOffDate).ToList();
		}
		int total = eloHistories.Count;
		if (total == 0) return 0.00;
		int wins = eloHistories.Count(eh => eh.Outcome == "win");
		double winRate = (double)wins / total * 100;
		return winRate;
	}

	public double GetAverageOpponentElo(List<EloHistory> eloHistories, int? days = null)
	{
		if (days.HasValue)
		{
			var cutOffDate = DateTime.UtcNow.AddDays(-days.Value);
			eloHistories = eloHistories.Where(eh => eh.ChangedAt >= cutOffDate).ToList();
		}

		var validEloEntries = eloHistories
			.Select(eh => eh.OpponentElo)
			.ToList();

		if (validEloEntries.Count == 0)
			return 0;

		return validEloEntries.Average();
	}

	public async Task<ThreeWayEloUpdateResponse> ResolveThreeWay(ThreeWayEloUpdateRequest twuReq)
	{
		// Validate input count
		if (twuReq.ThreeWayEloChanges == null || twuReq.ThreeWayEloChanges.Count != 3)
			throw new ArgumentException("Exactly 3 Elo changes required for three-way resolution.");

		// Validate roles exist exactly once
		var roles = twuReq.ThreeWayEloChanges.Select(c => c.Role).ToList();

		var requiredRoles = new[]
		{
			ThreeWayTranscriberRoleType.OriginalTranscriber1.ToDisplayName(),
			ThreeWayTranscriberRoleType.OriginalTranscriber2.ToDisplayName(),
			ThreeWayTranscriberRoleType.TiebreakerTranscriber.ToDisplayName()
		};

		foreach (var role in requiredRoles)
		{
			if (roles.Count(r => r == role) != 1)
				throw new ArgumentException($"Role '{role}' must appear exactly once in threeWayEloChanges.");
		}

		var userIds = twuReq.ThreeWayEloChanges.Select(t => t.TranscriberId).Distinct().ToList();
		var userStatsDb = await _userStatRepo.GetByUserIdsAsync(userIds, trackChanges: true);

		if (userStatsDb == null || userStatsDb.Count != userIds.Count)
			throw new Exception("Missing statistics for one or more transcribers.");

		// Find tiebreaker transcriber change
		var tiebreakerChange = twuReq.ThreeWayEloChanges
			.First(c => c.Role == ThreeWayTranscriberRoleType.TiebreakerTranscriber.ToDisplayName());


		var eloHistories = new List<EloHistory>();
		var updateResults = new List<EloUpdateResult>();
		var userNotifications = new List<UserNotification>();

		// We'll add tiebreaker's eloChange to original transcriber with minority outcome
		foreach (var change in twuReq.ThreeWayEloChanges.Where(c => c.Role != ThreeWayTranscriberRoleType.TiebreakerTranscriber.ToDisplayName()))
		{
			if (!EnumDisplayHelper.TryParseDisplayName(change.Role, out ThreeWayTranscriberRoleType roleEnum))
				throw new Exception($"Invalid user role provided");
			if (!EnumDisplayHelper.TryParseDisplayName(change.Outcome, out GameOutcomeType outcomeEnum))
				throw new Exception($"Invalid game outcome provided");

			var stats = userStatsDb.FirstOrDefault(u => u.UserId == change.TranscriberId)
				?? throw new Exception($"User stats not found for {change.TranscriberId}");

			var eloChangeAdjusted = change.EloChange;

			// If this original transcriber is a minority winner or minority loser, adjust eloChange by adding tiebreaker's eloChange
			if (outcomeEnum == GameOutcomeType.MinorityWinner || outcomeEnum == GameOutcomeType.MinorityLoser)
			{
				eloChangeAdjusted += tiebreakerChange.EloChange;
			}

			var oldEloVal = stats.CurrentElo;
			var newElo = oldEloVal + eloChangeAdjusted;

			stats.CurrentElo = newElo;
			stats.PeakElo = Math.Max(stats.PeakElo, newElo);
			stats.GamesPlayed++;
			stats.LastCalculated = DateTime.UtcNow;
			stats.UpdatedAt = DateTime.UtcNow;

			eloHistories.Add(new EloHistory
			{
				UserId = stats.UserId,
				OldElo = oldEloVal,
				NewElo = newElo,
				Reason = "three_way_resolution",
				Outcome = change.Outcome,
				ComparisonId = twuReq.OriginalComparisonId,
				JobId = twuReq.WorkflowRequestId,
				ComparisonType = "three_way",
				KFactorUsed = CalculateKFactor(stats.GamesPlayed),
				ChangedAt = DateTime.UtcNow
			});

			updateResults.Add(new EloUpdateResult
			{
				UserId = stats.UserId,
				NewElo = newElo,
				Change = eloChangeAdjusted
			});

			userNotifications.Add(new UserNotification
			{
				UserId = stats.UserId,
				NotificationType = eloChangeAdjusted > 0 ? "elo_increase" : "elo_decrease",
				Message = eloChangeAdjusted > 0
					? $"Great job! Your Elo rating increased by {eloChangeAdjusted} points to {newElo}."
					: $"Your Elo rating decreased by {Math.Abs(eloChangeAdjusted)} points to {newElo}. Review the feedback for improvement tips."
			});
		}

		// Handle tiebreaker transcriber bonus (no Elo update, just bonus if any)
		var tiebreakerStats = userStatsDb.First(u => u.UserId == tiebreakerChange.TranscriberId);

		if (tiebreakerChange.TiebreakerBonus?.Applied == true && tiebreakerChange.TiebreakerBonus.BonusAmount > 0)
		{
			var oldElo = tiebreakerStats.CurrentElo;
			tiebreakerStats.CurrentElo += tiebreakerChange.TiebreakerBonus.BonusAmount;
			tiebreakerStats.PeakElo = Math.Max(tiebreakerStats.PeakElo, tiebreakerStats.CurrentElo);
			tiebreakerStats.LastCalculated = DateTime.UtcNow;
			tiebreakerStats.UpdatedAt = DateTime.UtcNow;

			eloHistories.Add(new EloHistory
			{
				UserId = tiebreakerStats.UserId,
				OldElo = oldElo,
				NewElo = tiebreakerStats.CurrentElo,
				Reason = "tiebreaker_bonus",
				Outcome = tiebreakerChange.Outcome,
				ComparisonId = twuReq.TiebreakerComparisonId,
				JobId = twuReq.WorkflowRequestId,
				ComparisonType = "tiebreaker_bonus",
				KFactorUsed = 0,
				ChangedAt = DateTime.UtcNow
			});

			userNotifications.Add(new UserNotification
			{
				UserId = tiebreakerStats.UserId,
				NotificationType = "elo_bonus",
				Message = $"You received a bonus of {tiebreakerChange.TiebreakerBonus.BonusAmount} Elo for tiebreaker participation."
			});
		}

		_userStatRepo.UpdateRange(userStatsDb);
		await _unitOfWork.EloHistories.AddRangeAsync(eloHistories);
		await _unitOfWork.SaveChangesAsync();

		var notifyReq = new EloUpdateNotificationRequest
		{
			UpdateId = twuReq.WorkflowRequestId,
			EventType = WorkflowEventType.EloUpdated.ToDisplayName(),
			EventData = new EloUpdateEventData
			{
				ComparisonId = twuReq.OriginalComparisonId,
				UsersUpdated = eloHistories.Count,
				UpdateResults = updateResults
			}
		};

		await _workflowEngineClient.NotifyEloUpdatedAsync(notifyReq);

		return new ThreeWayEloUpdateResponse
		{
			EloUpdateConfirmed = true,
			UpdatesApplied = eloHistories.Count,
			Timestamp = DateTime.UtcNow,
			UserNotifications = userNotifications
		};
	}

}

using AutoMapper;
using CohesionX.UserManagement.Modules.Users.Application.Constants;
using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Persistence;
using CohesionX.UserManagement.Modules.Users.Persistence.Interfaces;

namespace CohesionX.UserManagement.Modules.Elo.Application.Services;

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
				OpponentId = eloChange.OpponentId,
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
			EventType = WorkflowEventType.ELO_UPDATED,
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
			Opponent = eh.OpponentId,
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
		var userIds = twuReq.ThreeWayEloChanges
			.Select(t => t.TranscriberId)
			.Distinct()
			.ToList();

		var userStatsDb = await _userStatRepo.GetByUserIdsAsync(userIds, trackChanges: true);

		if (userStatsDb == null || userStatsDb.Count != userIds.Count)
			throw new Exception("Missing statistics for one or more transcribers.");

		var eloHistories = new List<EloHistory>();
		var updateResults = new List<EloUpdateResult>();
		var userNotifications = new List<UserNotification>();

		foreach (var change in twuReq.ThreeWayEloChanges)
		{
			var stats = userStatsDb.FirstOrDefault(u => u.UserId == change.TranscriberId)
				?? throw new Exception($"User stats not found for {change.TranscriberId}");

			var newElo = change.NewElo;
			var delta = change.EloChange;

			// Update stats in-memory
			stats.CurrentElo = newElo;
			stats.PeakElo = Math.Max(stats.PeakElo, newElo);
			stats.GamesPlayed++;
			stats.LastCalculated = DateTime.UtcNow;
			stats.UpdatedAt = DateTime.UtcNow;

			eloHistories.Add(new EloHistory
			{
				UserId = change.TranscriberId,
				OldElo = newElo - delta,
				NewElo = newElo,
				OpponentElo = 0,
				OpponentId = change.OppenentId,
				OpponentId2 = change.OpponentId2,
				Reason = "three_way_resolution",
				ComparisonId = twuReq.OriginalComparisonId,
				JobId = twuReq.WorkflowRequestId,
				Outcome = change.Outcome,
				ComparisonType = "three_way",
				KFactorUsed = CalculateKFactor(stats.GamesPlayed),
				ChangedAt = DateTime.UtcNow
			});

			updateResults.Add(new EloUpdateResult
			{
				UserId = change.TranscriberId,
				NewElo = newElo,
				Change = delta
			});

			userNotifications.Add(new UserNotification
			{
				UserId = change.TranscriberId,
				NotificationType = delta >= 0 ? "elo_increase" : "elo_decrease",
				Message = delta >= 0
					? $"Great job! Your Elo rating increased by {delta} points to {newElo}."
					: $"Your Elo rating decreased by {Math.Abs(delta)} points to {newElo}. Review the feedback for improvement tips."
			});
		}

		_userStatRepo.UpdateRange(userStatsDb);
		await _unitOfWork.EloHistories.AddRangeAsync(eloHistories);
		await _unitOfWork.SaveChangesAsync();

		// Notify workflow engine
		var notifyReq = new EloUpdateNotificationRequest
		{
			UpdateId = twuReq.WorkflowRequestId,
			EventType = WorkflowEventType.ELO_UPDATED,
			EventData = new EloUpdateEventData
			{
				ComparisonId = twuReq.OriginalComparisonId,
				UsersUpdated = userIds.Count,
				UpdateResults = updateResults
			}
		};

		await _workflowEngineClient.NotifyEloUpdatedAsync(notifyReq);

		return new ThreeWayEloUpdateResponse
		{
			EloUpdateConfirmed = true,
			UpdatesApplied = updateResults.Count,
			Timestamp = DateTime.UtcNow,
			UserNotifications = userNotifications
		};
	}

}

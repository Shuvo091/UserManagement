using AutoMapper;
using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
using CohesionX.UserManagement.Modules.Users.Persistence;
using CohesionX.UserManagement.Shared.Persistence;

namespace CohesionX.UserManagement.Modules.Elo.Application.Services;

public class EloService : IEloService
{
	private readonly IEloRepository _repo;
	private readonly IUserRepository _userRepo;
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
		, IConfiguration config)
	{
		_repo = repo;
		_userRepo = userRepo;
		_unitOfWork = unitOfWork;
		_userStatRepo = userStatRepo;
		_mapper = mapper;
		_eloKFactorNew = int.TryParse(config["ELO_K_FACTOR_NEW"], out int parsedValue) ? parsedValue : 32;
		_eloKFactorEstablished = int.TryParse(config["ELO_K_FACTOR_ESTABLISHED"], out int parsedValue2) ? parsedValue2 : 24;
		_eloKFactorExpert = int.TryParse(config["ELO_K_FACTOR_EXPERT"], out int parsedValue3) ? parsedValue3 : 16;
	}

	public async Task<EloUpdateResponse> ApplyEloUpdatesAsync(EloUpdateRequest request)
	{
		var eloUpdateResp = new EloUpdateResponse
		{
			WorkflowRequestId = request.WorkflowRequestId,
			ComparisonId = request.QaComparisonId,
			UpdatedAt = DateTime.UtcNow
		};
		var userStatisticsDb = await _unitOfWork.UserStatistics.GetByUserIdsAsync(request.RecommendedEloChanges.Select(r => r.TranscriberId).ToList(), 
																		trackChanges: true);

		var eloHistoryRecords = new List<EloHistory>();

		if (userStatisticsDb == null 
			|| userStatisticsDb.Distinct().Count() 
				!= request.RecommendedEloChanges.Select(r => r.TranscriberId).Distinct().Count()
			) 
			throw new Exception($"User statistics not found all users");
		foreach (var eloChange in request.RecommendedEloChanges)
		{
			var userStatistics = userStatisticsDb.Where(us => us.UserId == eloChange.TranscriberId).FirstOrDefault() 
				?? throw new Exception($"User statistics not found for user {eloChange.TranscriberId}");
			var eloHistoryRecord = new EloHistory
			{
				UserId = eloChange.TranscriberId,
				OldElo = eloChange.OldElo,
				NewElo = eloChange.OldElo + eloChange.RecommendedChange,
				Reason = request.ComparisonMetadata.QaMethod ?? "",
				ComparisonId = request.QaComparisonId,
				JobId = request.WorkflowRequestId ?? "",
				Outcome = eloChange.ComparisonOutcome ?? "",
				ComparisonType = request.ComparisonMetadata.ComparisonType ?? "",
				KFactorUsed = CalculateKFactor(userStatistics.GamesPlayed),
				ChangedAt = DateTime.UtcNow
			};

			userStatistics.CurrentElo = eloHistoryRecord.NewElo;
			if (eloHistoryRecord.NewElo > userStatistics.PeakElo) userStatistics.PeakElo = eloHistoryRecord.NewElo;
			userStatistics.GamesPlayed++;
			userStatistics.LastCalculated = eloHistoryRecord.ChangedAt;
			userStatistics.UpdatedAt = DateTime.UtcNow;
			eloHistoryRecords.Add(eloHistoryRecord);

			eloUpdateResp.EloUpdatesApplied.Add(new EloUpdateAppliedDto
			{
				TranscriberId = eloChange.TranscriberId,
				OldElo = eloChange.OldElo,
				NewElo = eloHistoryRecord.NewElo,
				EloChange = eloHistoryRecord.NewElo - eloChange.OldElo,
				ComparisonOutcome = eloChange.ComparisonOutcome
			});
		}
		_unitOfWork.UserStatistics.UpdateRange(userStatisticsDb);
		await _unitOfWork.EloHistories.AddRangeAsync(eloHistoryRecords);
		await _unitOfWork.SaveChangesAsync();
		return eloUpdateResp;
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



	public async Task<EloHistoryDto[]> GetHistoryAsync(Guid userId)
	{
		var history = await _repo.GetByUserIdAsync(userId);
		return _mapper.Map<EloHistoryDto[]>(history);
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
}

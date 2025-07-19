using AutoMapper;
using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
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
				Reason = request.ComparisonMetadata.QaMethod,
				ComparisonId = request.QaComparisonId,
				JobId = request.WorkflowRequestId,
				Outcome = eloChange.ComparisonOutcome,
				ComparisonType = request.ComparisonMetadata.ComparisonType,
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

	private int CalculateKFactor(int gamesPlayed)
	{
		return gamesPlayed switch
		{
			< 30 => _eloKFactorNew,
			< 100 => _eloKFactorEstablished,
			_ => _eloKFactorExpert
		};
	}

	//public async Task<EloResultDto[]> ApplyEloUpdatesAsync(EloUpdateRequestDto req)
	//{
	//	var results = new List<EloResultDto>();

	//	foreach (var change in req.RecommendedEloChanges)
	//	{
	//		var user = await _repo.GetUserByIdAsync(change.TranscriberId);
	//		if (user == null) continue;

	//		int k = user.GamesPlayed switch
	//		{
	//			< 30 => 32,
	//			< 100 => 24,
	//			_ => 16
	//		};

	//		double expected = 1.0 / (1 + Math.Pow(10, (change.OpponentElo - user.EloRating) / 400.0));
	//		int newElo = (int)Math.Round(user.EloRating + k * ((change.Outcome == "win" ? 1 : 0) - expected));

	//		var eloHistory = new EloHistory
	//		{
	//			UserId = user.Id,
	//			OldElo = user.EloRating,
	//			NewElo = newElo,
	//			Reason = req.ComparisonMetadata.QaMethod,
	//			ComparisonId = req.ComparisonMetadata.ComparisonId,
	//			JobId = req.ComparisonMetadata.JobId,
	//			Outcome = change.Outcome,
	//			ComparisonType = req.ComparisonMetadata.ComparisonType,
	//			KFactorUsed = k,
	//			ChangedAt = DateTime.UtcNow
	//		};

	//		await _repo.AddEloHistoryAsync(eloHistory);

	//		user.EloRating = newElo;
	//		user.GamesPlayed++;

	//		results.Add(new EloResultDto
	//		{
	//			TranscriberId = user.Id,
	//			OldElo = change.OldElo,
	//			NewElo = newElo,
	//			EloChange = newElo - change.OldElo,
	//			Outcome = change.Outcome
	//		});
	//	}

	//	await _repo.SaveChangesAsync();
	//	return results.ToArray();
	//}

	public async Task<EloHistoryDto[]> GetHistoryAsync(Guid userId)
	{
		var history = await _repo.GetByUserIdAsync(userId);
		return _mapper.Map<EloHistoryDto[]>(history);
	}
}

using AutoMapper;
using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;

namespace CohesionX.UserManagement.Modules.Elo.Application.Services;

public class EloService : IEloService
{
	private readonly IEloRepository _repo;
	private readonly IMapper _mapper;

	public EloService(IEloRepository repo, IMapper mapper)
	{
		_repo = repo;
		_mapper = mapper;
	}

	public Task<EloResultDto[]> ApplyEloUpdatesAsync(EloUpdateRequestDto request)
	{
		throw new NotImplementedException();
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
		var history = await _repo.GetEloHistoryAsync(userId);
		return _mapper.Map<EloHistoryDto[]>(history);
	}
}

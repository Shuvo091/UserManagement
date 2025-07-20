using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;

namespace CohesionX.UserManagement.Modules.Users.Application.Interfaces;

public interface IEloService
{
	Task<EloUpdateResponse> ApplyEloUpdatesAsync(EloUpdateRequest request);
	Task<EloHistoryResponse> GetEloHistoryAsync(Guid userId);
	Task<string> GetEloTrend(Guid userId, int days);
	string GetEloTrend(List<EloHistory> eloHistories, int days);
	double GetWinRate(List<EloHistory> eloHistories, int? days = null);
	double GetAverageOpponentElo(List<EloHistory> eloHistories, int? days = null);
	Task<Dictionary<Guid, string>> BulkEloTrendAsync(List<Guid> userIds, int days);
}
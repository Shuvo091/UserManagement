using CohesionX.UserManagement.Domain.Entities;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Application.Interfaces;

public interface IEloService
{
	Task<EloUpdateResponse> ApplyEloUpdatesAsync(EloUpdateRequest request);
	Task<EloHistoryResponse> GetEloHistoryAsync(Guid userId);
	Task<ThreeWayEloUpdateResponse> ResolveThreeWay(ThreeWayEloUpdateRequest twuReq);
	Task<string> GetEloTrend(Guid userId, int days);
	string GetEloTrend(List<EloHistory> eloHistories, int days);
	double GetWinRate(List<EloHistory> eloHistories, int? days = null);
	double GetAverageOpponentElo(List<EloHistory> eloHistories, int? days = null);
	Task<Dictionary<Guid, string>> BulkEloTrendAsync(List<Guid> userIds, int days);
}
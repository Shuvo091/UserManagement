using CohesionX.UserManagement.Database.Abstractions.Entities;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Abstractions.Services;

/// <summary>
/// Provides operations for Elo rating management, history retrieval, and trend analysis.
/// </summary>
public interface IEloService
{
	/// <summary>
	/// Applies Elo updates based on the provided request.
	/// </summary>
	/// <param name="request">The Elo update request details.</param>
	/// <returns>The result of the Elo update operation.</returns>
	Task<EloUpdateResponse> ApplyEloUpdatesAsync(EloUpdateRequest request);

	/// <summary>
	/// Retrieves the Elo history for a specific user.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <returns>The Elo history response for the user.</returns>
	Task<EloHistoryResponse> GetEloHistoryAsync(Guid userId);

	/// <summary>
	/// Resolves a three-way Elo update scenario.
	/// </summary>
	/// <param name="twuReq">The three-way Elo update request details.</param>
	/// <returns>The result of the three-way resolution operation.</returns>
	Task<ThreeWayEloUpdateResponse> ResolveThreeWay(ThreeWayEloUpdateRequest twuReq);

	/// <summary>
	/// Gets the Elo trend for a user over a specified number of days.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <param name="days">The number of days to analyze.</param>
	/// <returns>A string representing the Elo trend.</returns>
	Task<string> GetEloTrend(Guid userId, int days);

	/// <summary>
	/// Gets the Elo trend for a list of Elo history records over a specified number of days.
	/// </summary>
	/// <param name="eloHistories">The list of Elo history records.</param>
	/// <param name="days">The number of days to analyze.</param>
	/// <returns>A string representing the Elo trend.</returns>
	string GetEloTrend(List<EloHistory> eloHistories, int days);

	/// <summary>
	/// Calculates the win rate from a list of Elo history records.
	/// </summary>
	/// <param name="eloHistories">The list of Elo history records.</param>
	/// <param name="days">Optional number of days to filter the records.</param>
	/// <returns>The win rate as a double.</returns>
	double GetWinRate(List<EloHistory> eloHistories, int? days = null);

	/// <summary>
	/// Calculates the average opponent Elo from a list of Elo history records.
	/// </summary>
	/// <param name="eloHistories">The list of Elo history records.</param>
	/// <param name="days">Optional number of days to filter the records.</param>
	/// <returns>The average opponent Elo as a double.</returns>
	double GetAverageOpponentElo(List<EloHistory> eloHistories, int? days = null);

	/// <summary>
	/// Gets Elo trends for multiple users over a specified number of days.
	/// </summary>
	/// <param name="userIds">The list of user identifiers.</param>
	/// <param name="days">The number of days to analyze.</param>
	/// <returns>A dictionary mapping user IDs to their Elo trend strings.</returns>
	Task<Dictionary<Guid, string>> BulkEloTrendAsync(List<Guid> userIds, int days);
}

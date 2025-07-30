using CohesionX.UserManagement.Database.Abstractions.Entities;

namespace CohesionX.UserManagement.Database.Abstractions.Repositories;

/// <summary>
/// Repository interface for managing <see cref="UserStatistics"/> entities,
/// extending the generic <see cref="IRepository{T}"/> interface.
/// </summary>
public interface IUserStatisticsRepository : IRepository<UserStatistics>
{
	/// <summary>
	/// Retrieves the statistics for a specific user by their unique identifier.
	/// </summary>
	/// <param name="userId">The unique identifier of the user.</param>
	/// <param name="trackChanges">
	/// Indicates whether to track changes on the retrieved entity.
	/// Defaults to <c>false</c>.
	/// </param>
	/// <returns>
	/// A task representing the asynchronous operation, containing the <see cref="UserStatistics"/> if found; otherwise, <c>null</c>.
	/// </returns>
	Task<UserStatistics?> GetByUserIdAsync(Guid userId, bool trackChanges = false);

	/// <summary>
	/// Retrieves statistics for multiple users by their unique identifiers.
	/// </summary>
	/// <param name="userIds">The list of user unique identifiers.</param>
	/// <param name="trackChanges">
	/// Indicates whether to track changes on the retrieved entities.
	/// Defaults to <c>false</c>.
	/// </param>
	/// <returns>
	/// A task representing the asynchronous operation, containing a list of <see cref="UserStatistics"/> records.
	/// </returns>
	Task<List<UserStatistics>> GetByUserIdsAsync(List<Guid> userIds, bool trackChanges = false);
}

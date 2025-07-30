using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Abstractions.Services;

/// <summary>
/// Provides operations for managing user availability, job claims, and Elo data in Redis.
/// </summary>
public interface IRedisService
{
	/// <summary>
	/// Gets the availability information for a user from Redis.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <returns>The user's availability data, or <c>null</c> if not found.</returns>
	Task<UserAvailabilityRedisDto?> GetAvailabilityAsync(Guid userId);

	/// <summary>
	/// Sets the availability information for a user in Redis.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <param name="dto">The availability data to store.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	Task SetAvailabilityAsync(Guid userId, UserAvailabilityRedisDto dto);

	/// <summary>
	/// Attempts to claim a job for a user in Redis.
	/// </summary>
	/// <param name="jobId">The job's unique identifier.</param>
	/// <param name="userId">The user's unique identifier.</param>
	/// <returns><c>true</c> if the job was successfully claimed; otherwise, <c>false</c>.</returns>
	Task<bool> TryClaimJobAsync(string jobId, Guid userId);

	/// <summary>
	/// Releases a previously claimed job in Redis.
	/// </summary>
	/// <param name="jobId">The job's unique identifier.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	Task ReleaseJobClaimAsync(string jobId);

	/// <summary>
	/// Gets the list of job IDs currently claimed by a user.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <returns>A list of job IDs claimed by the user.</returns>
	Task<List<string>> GetUserClaimsAsync(Guid userId);

	/// <summary>
	/// Adds a job claim for a user in Redis.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <param name="jobId">The job's unique identifier.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	Task AddUserClaimAsync(Guid userId, string jobId);

	/// <summary>
	/// Removes a job claim for a user in Redis.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <param name="jobId">The job's unique identifier.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	Task RemoveUserClaimAsync(Guid userId, string jobId);

	/// <summary>
	/// Gets the Elo data for a user from Redis.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <returns>The user's Elo data, or <c>null</c> if not found.</returns>
	Task<UserEloRedisDto?> GetUserEloAsync(Guid userId);

	/// <summary>
	/// Sets the Elo data for a user in Redis.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <param name="dto">The Elo data to store.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	Task SetUserEloAsync(Guid userId, UserEloRedisDto dto);

	/// <summary>
	/// Gets the availability information for multiple users from Redis.
	/// </summary>
	/// <param name="userIds">The collection of user identifiers.</param>
	/// <returns>A dictionary mapping user IDs to their availability data.</returns>
	Task<Dictionary<Guid, UserAvailabilityRedisDto>> GetBulkAvailabilityAsync(IEnumerable<Guid> userIds);
}

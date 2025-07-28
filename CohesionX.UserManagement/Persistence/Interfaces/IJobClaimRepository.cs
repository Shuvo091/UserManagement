using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.UserManagement.Persistence.Interfaces;

/// <summary>
/// Repository interface for managing <see cref="JobClaim"/> entities,
/// extending the generic <see cref="IRepository{T}"/> interface.
/// </summary>
public interface IJobClaimRepository : IRepository<JobClaim>
{
	/// <summary>
	/// Adds a new <see cref="JobClaim"/> record asynchronously.
	/// </summary>
	/// <param name="jobClaim">The job claim entity to add.</param>
	/// <returns>A task representing the asynchronous operation, containing the added <see cref="JobClaim"/>.</returns>
	Task<JobClaim> AddJobClaimAsync(JobClaim jobClaim);

	/// <summary>
	/// Retrieves a <see cref="JobClaim"/> by its associated job ID.
	/// </summary>
	/// <param name="jobId">The unique identifier of the job.</param>
	/// <param name="trackChanges">
	/// Indicates whether to track changes on the retrieved entity.
	/// Defaults to <c>false</c>.
	/// </param>
	/// <returns>
	/// A task representing the asynchronous operation, containing the <see cref="JobClaim"/> if found; otherwise, <c>null</c>.
	/// </returns>
	Task<JobClaim?> GetJobClaimByJobId(string jobId, bool trackChanges = false);
}

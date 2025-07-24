using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.UserManagement.Persistence.Interfaces;

public interface IJobClaimRepository : IRepository<JobClaim>
{
	Task<JobClaim> AddJobClaimAsync(JobClaim jobClaim);
	Task<JobClaim?> GetJobClaimByJobId(string jobId, bool trackChanges = false);
}

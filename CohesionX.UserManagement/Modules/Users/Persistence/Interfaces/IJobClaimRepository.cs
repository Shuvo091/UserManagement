using CohesionX.UserManagement.Modules.Users.Domain.Entities;

namespace CohesionX.UserManagement.Modules.Users.Persistence.Interfaces;

public interface IJobClaimRepository : IRepository<JobClaim>
{
	Task<JobClaim> AddJobClaimAsync(JobClaim jobClaim);
}

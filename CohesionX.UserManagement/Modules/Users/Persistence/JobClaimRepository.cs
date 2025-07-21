using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Persistence.Interfaces;
using CohesionX.UserManagement.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Modules.Users.Persistence;

public class JobClaimRepository : Repository<JobClaim>, IJobClaimRepository
{
	private readonly AppDbContext _context;

	public JobClaimRepository(AppDbContext context) : base(context)
	{
		_context = context;
	}

	public async Task<JobClaim> AddJobClaimAsync(JobClaim jobClaim)
	{
		await _context.JobClaims.AddAsync(jobClaim);
		return jobClaim;
	}
}
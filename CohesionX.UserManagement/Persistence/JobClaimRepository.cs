using CohesionX.UserManagement.Domain.Entities;
using CohesionX.UserManagement.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Persistence;

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

	public async Task<JobClaim?> GetJobClaimByJobId(string jobId, bool trackChanges = false)
	{
		return trackChanges ?
			await _context.JobClaims.Include(j => j.User).FirstOrDefaultAsync(j => j.JobId == jobId)
			: await _context.JobClaims.Include(j => j.User).AsNoTracking().FirstOrDefaultAsync(j => j.JobId == jobId);
	}
}
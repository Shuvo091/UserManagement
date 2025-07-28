using CohesionX.UserManagement.Domain.Entities;
using CohesionX.UserManagement.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Persistence
{
	/// <summary>
	/// Repository implementation for managing <see cref="JobClaim"/> entities.
	/// Inherits from the generic <see cref="Repository{T}"/> base class.
	/// </summary>
	public class JobClaimRepository : Repository<JobClaim>, IJobClaimRepository
	{
		private readonly AppDbContext _context;

		/// <summary>
		/// Initializes a new instance of the <see cref="JobClaimRepository"/> class with the specified database context.
		/// </summary>
		/// <param name="context">The application database context.</param>
		public JobClaimRepository(AppDbContext context) : base(context)
		{
			_context = context;
		}

		/// <summary>
		/// Adds a new <see cref="JobClaim"/> entity asynchronously to the database context.
		/// </summary>
		/// <param name="jobClaim">The job claim entity to add.</param>
		/// <returns>
		/// A task representing the asynchronous operation, returning the added <see cref="JobClaim"/>.
		/// </returns>
		public async Task<JobClaim> AddJobClaimAsync(JobClaim jobClaim)
		{
			await _context.JobClaims.AddAsync(jobClaim);
			return jobClaim;
		}

		/// <summary>
		/// Retrieves a <see cref="JobClaim"/> by its associated job ID, optionally tracking changes.
		/// Includes the related <see cref="User"/> entity.
		/// </summary>
		/// <param name="jobId">The job identifier.</param>
		/// <param name="trackChanges">
		/// If <c>true</c>, the returned entity will be tracked by the context; otherwise, no tracking is applied.
		/// </param>
		/// <returns>
		/// A task representing the asynchronous operation, containing the matching <see cref="JobClaim"/> if found; otherwise, <c>null</c>.
		/// </returns>
		public async Task<JobClaim?> GetJobClaimByJobId(string jobId, bool trackChanges = false)
		{
			return trackChanges
				? await _context.JobClaims.Include(j => j.User).FirstOrDefaultAsync(j => j.JobId == jobId)
				: await _context.JobClaims.Include(j => j.User).AsNoTracking().FirstOrDefaultAsync(j => j.JobId == jobId);
		}
	}
}

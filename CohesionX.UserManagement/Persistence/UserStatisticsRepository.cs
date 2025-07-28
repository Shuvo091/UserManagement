using CohesionX.UserManagement.Domain.Entities;
using CohesionX.UserManagement.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Persistence
{
	/// <summary>
	/// Repository implementation for managing <see cref="UserStatistics"/> entities.
	/// Provides methods to retrieve user statistics by user ID(s) with optional tracking.
	/// </summary>
	public class UserStatisticsRepository : Repository<UserStatistics>, IUserStatisticsRepository
	{
		private readonly AppDbContext _context;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserStatisticsRepository"/> class
		/// with the specified database context.
		/// </summary>
		/// <param name="context">The application database context.</param>
		public UserStatisticsRepository(AppDbContext context)
            : base(context)
		{
			_context = context;
		}

		/// <summary>
		/// Retrieves a single <see cref="UserStatistics"/> entity by the specified user ID asynchronously.
		/// </summary>
		/// <param name="userId">The unique identifier of the user.</param>
		/// <param name="trackChanges">If <c>true</c>, tracks changes on the returned entity; otherwise, no tracking is applied.</param>
		/// <returns>A task representing the asynchronous operation, containing the matching <see cref="UserStatistics"/> or <c>null</c> if not found.</returns>
		public async Task<UserStatistics?> GetByUserIdAsync(Guid userId, bool trackChanges = false)
		{
			return trackChanges
				? await _context.UserStatistics.FirstOrDefaultAsync(us => us.UserId == userId)
				: await _context.UserStatistics.AsNoTracking().FirstOrDefaultAsync(us => us.UserId == userId);
		}

		/// <summary>
		/// Retrieves a list of <see cref="UserStatistics"/> entities for the specified user IDs asynchronously.
		/// </summary>
		/// <param name="userIds">A list of user IDs to retrieve statistics for.</param>
		/// <param name="trackChanges">If <c>true</c>, tracks changes on the returned entities; otherwise, no tracking is applied.</param>
		/// <returns>A task representing the asynchronous operation, containing the list of matching <see cref="UserStatistics"/> entities.</returns>
		public async Task<List<UserStatistics>> GetByUserIdsAsync(List<Guid> userIds, bool trackChanges = false)
		{
			return trackChanges
				? await _context.UserStatistics.Where(us => userIds.Contains(us.UserId)).ToListAsync()
				: await _context.UserStatistics.AsNoTracking().Where(us => userIds.Contains(us.UserId)).ToListAsync();
		}
	}
}

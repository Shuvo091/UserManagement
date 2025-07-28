using CohesionX.UserManagement.Domain.Entities;
using CohesionX.UserManagement.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CohesionX.UserManagement.Persistence
{
	/// <summary>
	/// Repository implementation for managing <see cref="User"/> entities.
	/// Provides methods for querying users with optional eager loading of related entities.
	/// </summary>
	public class UserRepository : Repository<User>, IUserRepository
	{
		private readonly AppDbContext _context;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserRepository"/> class with the specified database context.
		/// </summary>
		/// <param name="context">The application database context.</param>
		public UserRepository(AppDbContext context) : base(context)
		{
			_context = context;
		}

		/// <summary>
		/// Checks asynchronously whether an email address is already registered.
		/// </summary>
		/// <param name="email">The email address to check.</param>
		/// <returns>A task representing the asynchronous operation, containing <c>true</c> if the email exists; otherwise, <c>false</c>.</returns>
		public Task<bool> EmailExistsAsync(string email)
			=> _context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant());

		/// <summary>
		/// Retrieves a user by their unique identifier asynchronously, optionally including related data.
		/// </summary>
		/// <param name="userId">The unique identifier of the user.</param>
		/// <param name="includeRelated">
		/// If <c>true</c>, includes related entities such as dialects, statistics, Elo histories, job completions, job claims, audit logs, and verification records.
		/// </param>
		/// <returns>A task representing the asynchronous operation, containing the user if found; otherwise, <c>null</c>.</returns>
		public async Task<User?> GetUserByIdAsync(Guid userId, bool includeRelated = false)
		{
			if (!includeRelated)
				return await _context.Users
					.Include(u => u.Statistics)
					.FirstOrDefaultAsync(u => u.Id == userId);

			return await _context.Users
				.Include(u => u.Dialects)
				.Include(u => u.Statistics)
				.Include(u => u.EloHistories)
				.Include(u => u.JobCompletions)
				.Include(u => u.JobClaims)
				.Include(u => u.AuditLogs)
				.Include(u => u.VerificationRecords)
				.FirstOrDefaultAsync(u => u.Id == userId);
		}

		/// <summary>
		/// Retrieves a user by their email address asynchronously, optionally including related data.
		/// </summary>
		/// <param name="email">The email address of the user.</param>
		/// <param name="includeRelated">
		/// If <c>true</c>, includes related entities such as dialects, statistics, Elo histories, job completions, job claims, audit logs, and verification records.
		/// </param>
		/// <returns>A task representing the asynchronous operation, containing the user if found; otherwise, <c>null</c>.</returns>
		public async Task<User?> GetUserByEmailAsync(string email, bool includeRelated = false)
		{
			if (!includeRelated)
				return await _context.Users.Where(u => u.Email == email).FirstOrDefaultAsync();

			return await _context.Users
				.Include(u => u.Dialects)
				.Include(u => u.Statistics)
				.Include(u => u.EloHistories)
				.Include(u => u.JobCompletions)
				.Include(u => u.JobClaims)
				.Include(u => u.AuditLogs)
				.Include(u => u.VerificationRecords)
				.FirstOrDefaultAsync(u => u.Email == email);
		}

		/// <summary>
		/// Retrieves all users asynchronously, optionally including related data.
		/// </summary>
		/// <param name="includeRelated">
		/// If <c>true</c>, includes related entities such as dialects, statistics, Elo histories, job completions, job claims, audit logs, and verification records.
		/// </param>
		/// <returns>A task representing the asynchronous operation, containing a list of users.</returns>
		public Task<List<User>> GetAllUsers(bool includeRelated = false)
		{
			if (!includeRelated)
				return _context.Users.AsNoTracking().ToListAsync();

			return _context.Users
				.Include(u => u.Dialects)
				.Include(u => u.Statistics)
				.Include(u => u.EloHistories)
				.Include(u => u.JobCompletions)
				.Include(u => u.JobClaims)
				.Include(u => u.AuditLogs)
				.Include(u => u.VerificationRecords)
				.AsNoTracking()
				.ToListAsync();
		}

		/// <summary>
		/// Retrieves a filtered list of users matching the specified predicate asynchronously.
		/// </summary>
		/// <param name="predicate">The filter expression.</param>
		/// <returns>A task representing the asynchronous operation, containing the list of filtered users.</returns>
		public Task<List<User>> GetFilteredListAsync(Expression<Func<User, bool>> predicate)
		{
			return _context.Users.AsNoTracking().Where(predicate).ToListAsync();
		}

		/// <summary>
		/// Retrieves a filtered and projected list of users asynchronously.
		/// </summary>
		/// <typeparam name="T">The projection type.</typeparam>
		/// <param name="predicate">The filter expression.</param>
		/// <param name="selector">The projection expression.</param>
		/// <returns>A task representing the asynchronous operation, containing the projected list.</returns>
		public Task<List<T>> GetFilteredListProjectedToAsync<T>(Expression<Func<User, bool>> predicate, Expression<Func<User, T>> selector)
		{
			return _context.Users
				.AsNoTracking()
				.Where(predicate)
				.Select(selector)
				.ToListAsync();
		}

		/// <summary>
		/// Retrieves a filtered list of users based on optional criteria such as dialect, Elo rating range, maximum workload, and result limit.
		/// </summary>
		/// <param name="dialect">The dialect to filter by (optional).</param>
		/// <param name="minElo">The minimum Elo rating (optional).</param>
		/// <param name="maxElo">The maximum Elo rating (optional).</param>
		/// <param name="maxWorkload">The maximum number of active job claims (optional).</param>
		/// <param name="limit">The maximum number of results to return (optional).</param>
		/// <returns>A task representing the asynchronous operation, containing the list of filtered users.</returns>
		public async Task<List<User>> GetFilteredUser(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit)
		{
			var query = _context.Users
				.Include(u => u.Dialects)
				.Include(u => u.Statistics)
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(dialect))
				query = query.Where(u => u.Dialects.Any(d => d.Dialect == dialect));

			if (minElo.HasValue)
				query = query.Where(u => u.Statistics.CurrentElo >= minElo.Value);

			if (maxElo.HasValue)
				query = query.Where(u => u.Statistics.CurrentElo <= maxElo.Value);

			if (maxWorkload.HasValue)
				query = query.Where(u => u.JobClaims.Count <= maxWorkload.Value);

			if (limit.HasValue)
				query = query.Take(limit.Value);

			return await query.ToListAsync();
		}
	}
}

using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
using CohesionX.UserManagement.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CohesionX.UserManagement.Modules.Users.Persistence;

public class UserRepository : Repository<User>, IUserRepository
{
	private readonly AppDbContext _context;

	public UserRepository(AppDbContext context) : base(context)
	{
		_context = context;
	}

	public Task<bool> EmailExistsAsync(string email)
		=> _context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant());

	public async Task<User?> GetUserByIdAsync(Guid userId, bool includeRelated = false)
	{
		if (!includeRelated)
			return await _context.Users.FindAsync(userId);

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

	public Task<List<User>> GetAllUsers(bool includeRelated = false)
	{
		if (!includeRelated) return _context.Users.AsNoTracking().ToListAsync();

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

	public Task<List<User>> GetFilteredListAsync(Expression<Func<User, bool>> predicate)
	{
		return _context.Users.AsNoTracking().Where(predicate).ToListAsync();
	}

	public Task<List<T>> GetFilteredListProjectedToAsync<T>(Expression<Func<User, bool>> predicate, Expression<Func<User, T>> selector)
	{
		return _context.Users
			.AsNoTracking()
			.Where(predicate)
			.Select(selector)
			.ToListAsync();
	}

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

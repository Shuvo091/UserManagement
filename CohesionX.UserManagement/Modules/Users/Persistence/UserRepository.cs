using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
using CohesionX.UserManagement.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CohesionX.UserManagement.Modules.Users.Persistence;

public class UserRepository : IUserRepository
{
	private readonly AppDbContext _context;

	public UserRepository(AppDbContext context)
	{
		_context = context;
	}

	public Task AddAsync(User user) =>
		_context.Users.AddAsync(user).AsTask();

	public Task<bool> EmailExistsAsync(string email) =>
		_context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant());

	public Task<User?> GetUserByIdAsync(Guid userId, bool includeRelated = false)
	{
		if (!includeRelated)
			return _context.Users.FindAsync(userId).AsTask();

		return _context.Users
			.Include(u => u.Dialects)
			.Include(u => u.Statistics)
			.Include(u => u.EloHistories)
			.Include(u => u.JobCompletions)
			.Include(u => u.JobClaims)
			.Include(u => u.AuditLogs)
			.Include(u => u.VerificationRecords)
			.FirstOrDefaultAsync(u => u.Id == userId);
	}

	public async Task<List<User>> GetFilteredListAsync(Expression<Func<User, bool>> predicate)
	{
		return await _context.Users.AsNoTracking().Where(predicate).ToListAsync();
	}

	public async Task<List<T>> GetFilteredListProjectedToAsync<T>(Expression<Func<User, bool>> predicate, Expression<Func<User, T>> selector)
	{
		return await _context.Users.AsNoTracking().Where(predicate).Select(selector).ToListAsync();
	}

	public async Task<List<UserWithEloAndDialectsDto>> GetUsersWithEloAndDialectsAsync()
	{
		return await _context.Users
			.Include(u => u.Dialects)
			.Select(u => new UserWithEloAndDialectsDto
			{
				UserId = u.Id,
				DialectCodes = u.Dialects.Select(d => d.Dialect).ToList(),
				IsProfessional = u.IsProfessional,
				UserRole = ""
			})
			.ToListAsync();
	}

	public Task SaveChangesAsync() =>
		_context.SaveChangesAsync();
}

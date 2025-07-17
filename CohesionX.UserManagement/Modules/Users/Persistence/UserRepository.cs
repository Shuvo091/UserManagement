using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
using CohesionX.UserManagement.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

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

	public Task SaveChangesAsync() =>
		_context.SaveChangesAsync();
}

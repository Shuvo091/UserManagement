using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
using CohesionX.UserManagement.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Modules.Users.Persistence;

public class EloRepository : IEloRepository
{
	private readonly AppDbContext _db;

	public EloRepository(AppDbContext db)
	{
		_db = db;
	}

	public async Task<User?> GetUserByIdAsync(Guid userId)
	{
		return await _db.Users.FindAsync(userId);
	}

	public async Task<List<User>> GetUsersByIdsAsync(IEnumerable<Guid> ids)
	{
		return await _db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
	}

	public async Task AddEloHistoryAsync(EloHistory history)
	{
		await _db.EloHistories.AddAsync(history);
	}

	public async Task<List<EloHistory>> GetEloHistoryAsync(Guid userId)
	{
		return await _db.EloHistories
			.Where(h => h.UserId == userId)
			.OrderByDescending(h => h.ChangedAt)
			.ToListAsync();
	}

	public async Task SaveChangesAsync()
	{
		await _db.SaveChangesAsync();
	}
}

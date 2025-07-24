using CohesionX.UserManagement.Domain.Entities;
using CohesionX.UserManagement.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Persistence;

public class UserStatisticsRepository : Repository<UserStatistics>, IUserStatisticsRepository
{
	private readonly AppDbContext _context;

	public UserStatisticsRepository(AppDbContext context) : base(context)
	{
		_context = context;
	}

	public async Task<UserStatistics?> GetByUserIdAsync(Guid userId, bool trackChanges = false)
	{
		return trackChanges
			? await _context.UserStatistics.FirstOrDefaultAsync(us => us.UserId == userId)
			: await _context.UserStatistics.AsNoTracking().FirstOrDefaultAsync(us => us.UserId == userId);
	}

	public async Task<List<UserStatistics>> GetByUserIdsAsync(List<Guid> userIds, bool trackChanges = false)
	{
		return trackChanges
			? await _context.UserStatistics.Where(us => userIds.Contains(us.UserId)).ToListAsync()
			: await _context.UserStatistics.AsNoTracking().Where(us => userIds.Contains(us.UserId)).ToListAsync();
	}
}

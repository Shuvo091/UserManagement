using CohesionX.UserManagement.Persistence.Interfaces;

namespace CohesionX.UserManagement.Persistence;

public class UnitOfWork : IUnitOfWork
{
	private readonly AppDbContext _context;

	public IEloRepository EloHistories { get; }
	public IUserStatisticsRepository UserStatistics { get; }
	public IUserRepository Users { get; }

	public UnitOfWork(AppDbContext context)
	{
		_context = context;
		EloHistories = new EloRepository(_context);
		UserStatistics = new UserStatisticsRepository(_context);
		Users = new UserRepository(_context);
	}

	public Task<int> SaveChangesAsync()
		=> _context.SaveChangesAsync();
}

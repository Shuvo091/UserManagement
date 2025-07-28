using CohesionX.UserManagement.Persistence.Interfaces;

namespace CohesionX.UserManagement.Persistence
{
	/// <summary>
	/// Implements the Unit of Work pattern to coordinate multiple repository operations
	/// and manage database transactions for the User Management module.
	/// </summary>
	public class UnitOfWork : IUnitOfWork
	{
		private readonly AppDbContext _context;

		/// <summary>
		/// Gets the repository for managing Elo history records.
		/// </summary>
		public IEloRepository EloHistories { get; }

		/// <summary>
		/// Gets the repository for managing user statistics entities.
		/// </summary>
		public IUserStatisticsRepository UserStatistics { get; }

		/// <summary>
		/// Gets the repository for managing user entities.
		/// </summary>
		public IUserRepository Users { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="UnitOfWork"/> class
		/// with the specified application database context.
		/// </summary>
		/// <param name="context">The application database context.</param>
		public UnitOfWork(AppDbContext context)
		{
			_context = context;
			EloHistories = new EloRepository(_context);
			UserStatistics = new UserStatisticsRepository(_context);
			Users = new UserRepository(_context);
		}

		/// <summary>
		/// Persists all changes made in the context to the database asynchronously.
		/// </summary>
		/// <returns>
		/// A task representing the asynchronous save operation, 
		/// returning the number of state entries written to the database.
		/// </returns>
		public Task<int> SaveChangesAsync()
			=> _context.SaveChangesAsync();
	}
}

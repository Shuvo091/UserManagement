using System.Threading.Tasks;

namespace CohesionX.UserManagement.Persistence.Interfaces;

public interface IUnitOfWork
{
	IEloRepository EloHistories { get; }
	IUserStatisticsRepository UserStatistics { get; }
	IUserRepository Users { get; }

	Task<int> SaveChangesAsync();
}

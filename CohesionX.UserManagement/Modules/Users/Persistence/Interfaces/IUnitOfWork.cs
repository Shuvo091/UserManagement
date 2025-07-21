using System.Threading.Tasks;

namespace CohesionX.UserManagement.Modules.Users.Persistence.Interfaces;

public interface IUnitOfWork
{
	IEloRepository EloHistories { get; }
	IUserStatisticsRepository UserStatistics { get; }
	IUserRepository Users { get; }

	Task<int> SaveChangesAsync();
}

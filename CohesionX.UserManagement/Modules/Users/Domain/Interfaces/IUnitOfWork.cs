using System.Threading.Tasks;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;

namespace CohesionX.UserManagement.Shared.Persistence;

public interface IUnitOfWork
{
	IEloRepository EloHistories { get; }
	IUserStatisticsRepository UserStatistics { get; }
	IUserRepository Users { get; }

	Task<int> SaveChangesAsync();
}

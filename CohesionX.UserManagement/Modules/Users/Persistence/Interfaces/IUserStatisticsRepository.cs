using CohesionX.UserManagement.Modules.Users.Domain.Entities;

namespace CohesionX.UserManagement.Modules.Users.Persistence.Interfaces;

public interface IUserStatisticsRepository : IRepository<UserStatistics>
{
	Task<UserStatistics?> GetByUserIdAsync(Guid userId, bool trackChanges = false);
	Task<List<UserStatistics>> GetByUserIdsAsync(List<Guid> userIds, bool trackChanges = false);
}
using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.UserManagement.Persistence.Interfaces;

public interface IUserStatisticsRepository : IRepository<UserStatistics>
{
	Task<UserStatistics?> GetByUserIdAsync(Guid userId, bool trackChanges = false);
	Task<List<UserStatistics>> GetByUserIdsAsync(List<Guid> userIds, bool trackChanges = false);
}
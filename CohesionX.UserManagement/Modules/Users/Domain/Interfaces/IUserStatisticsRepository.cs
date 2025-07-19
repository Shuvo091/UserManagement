using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Shared.Persistence;

namespace CohesionX.UserManagement.Modules.Users.Domain.Interfaces;

public interface IUserStatisticsRepository : IRepository<UserStatistics>
{
	Task<UserStatistics?> GetByUserIdAsync(Guid userId, bool trackChanges = false);
	Task<List<UserStatistics>> GetByUserIdsAsync(List<Guid> userIds, bool trackChanges = false);
}
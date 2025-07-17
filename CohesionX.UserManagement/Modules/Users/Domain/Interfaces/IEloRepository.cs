using CohesionX.UserManagement.Modules.Users.Domain.Entities;

namespace CohesionX.UserManagement.Modules.Users.Domain.Interfaces;

public interface IEloRepository
{
	Task<User?> GetUserByIdAsync(Guid userId);
	Task<List<User>> GetUsersByIdsAsync(IEnumerable<Guid> ids);
	Task AddEloHistoryAsync(EloHistory history);
	Task SaveChangesAsync();
	Task<List<EloHistory>> GetEloHistoryAsync(Guid userId);
}
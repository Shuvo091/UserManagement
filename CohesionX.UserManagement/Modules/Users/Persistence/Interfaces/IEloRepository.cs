using CohesionX.UserManagement.Modules.Users.Domain.Entities;

namespace CohesionX.UserManagement.Modules.Users.Persistence.Interfaces;

public interface IEloRepository : IRepository<EloHistory>
{
	Task<List<EloHistory>> GetByUserIdAsync(Guid userId);
}

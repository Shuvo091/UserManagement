using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Shared.Persistence;

namespace CohesionX.UserManagement.Modules.Users.Domain.Interfaces;

public interface IEloRepository : IRepository<EloHistory>
{
	Task<List<EloHistory>> GetByUserIdAsync(Guid userId);
}

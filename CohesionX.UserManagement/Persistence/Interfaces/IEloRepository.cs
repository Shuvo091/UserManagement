using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.UserManagement.Persistence.Interfaces;

public interface IEloRepository : IRepository<EloHistory>
{
	Task<List<EloHistory>> GetByUserIdAsync(Guid userId);
}

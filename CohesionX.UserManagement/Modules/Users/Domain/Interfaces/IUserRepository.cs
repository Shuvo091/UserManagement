using CohesionX.UserManagement.Modules.Users.Domain.Entities;

namespace CohesionX.UserManagement.Modules.Users.Domain.Interfaces;

public interface IUserRepository
{
	Task AddAsync(User user);
	Task SaveChangesAsync();
}

using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;

namespace CohesionX.UserManagement.Modules.Users.Domain.Interfaces;

public interface IUserRepository
{
	Task AddAsync(User user);
	Task<User?> GetUserByIdAsync(Guid userId, bool includeRelated = false);
	Task SaveChangesAsync();
	Task<bool> EmailExistsAsync(string email);
	Task<List<User>> GetFilteredListAsync(System.Linq.Expressions.Expression<Func<User, bool>> predicate);
	Task<List<T>> GetFilteredListProjectedToAsync<T>(System.Linq.Expressions.Expression<Func<User, bool>> predicate, System.Linq.Expressions.Expression<Func<User, T>> selector);
	Task<List<UserWithEloAndDialectsDto>> GetUsersWithEloAndDialectsAsync();
}

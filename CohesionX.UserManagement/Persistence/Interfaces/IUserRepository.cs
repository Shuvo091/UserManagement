using CohesionX.UserManagement.Domain.Entities;
using System.Linq.Expressions;

namespace CohesionX.UserManagement.Persistence.Interfaces;

public interface IUserRepository : IRepository<User>
{
	Task<bool> EmailExistsAsync(string email);
	Task<User?> GetUserByIdAsync(Guid userId, bool includeRelated = false);
	Task<User?> GetUserByEmailAsync(string email, bool includeRelated = false);
	Task<List<User>> GetFilteredListAsync(Expression<Func<User, bool>> predicate);
	Task<List<T>> GetFilteredListProjectedToAsync<T>(Expression<Func<User, bool>> predicate, Expression<Func<User, T>> selector);
	Task<List<User>> GetFilteredUser(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit);
	Task<List<User>> GetAllUsers(bool includeRelated = false);
}
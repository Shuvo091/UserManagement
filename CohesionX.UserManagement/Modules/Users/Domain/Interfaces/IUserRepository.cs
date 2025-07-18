using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CohesionX.UserManagement.Modules.Users.Domain.Interfaces;

public interface IUserRepository
{
	Task AddAsync(User user);
	Task UpdateAsync(User user);
	Task<User?> GetUserByIdAsync(Guid userId, bool includeRelated = false);
	Task SaveChangesAsync();
	Task<bool> EmailExistsAsync(string email);
	Task<List<User>> GetFilteredListAsync(Expression<Func<User, bool>> predicate);
	Task<List<T>> GetFilteredListProjectedToAsync<T>(Expression<Func<User, bool>> predicate, Expression<Func<User, T>> selector);
	Task<List<User>> GetAll(bool includeRelated = false);
	Task<List<User>> GetFilteredUser(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit);
}

using System.Linq.Expressions;
using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.UserManagement.Persistence.Interfaces;

/// <summary>
/// Repository interface for managing <see cref="User"/> entities,
/// extending the generic <see cref="IRepository{T}"/> interface.
/// </summary>
public interface IUserRepository : IRepository<User>
{
	/// <summary>
	/// Checks asynchronously whether a user with the specified email exists.
	/// </summary>
	/// <param name="email">The email address to check.</param>
	/// <returns>A task representing the asynchronous operation, containing <c>true</c> if the email exists; otherwise, <c>false</c>.</returns>
	Task<bool> EmailExistsAsync(string email);

	/// <summary>
	/// Retrieves a user by their unique identifier, optionally including related entities.
	/// </summary>
	/// <param name="userId">The unique identifier of the user.</param>
	/// <param name="includeRelated">If <c>true</c>, related navigation properties will be included.</param>
	/// <returns>A task representing the asynchronous operation, containing the user if found; otherwise, <c>null</c>.</returns>
	Task<User?> GetUserByIdAsync(Guid userId, bool includeRelated = false);

	/// <summary>
	/// Retrieves a user by their email address, optionally including related entities.
	/// </summary>
	/// <param name="email">The email address of the user.</param>
	/// <param name="includeRelated">If <c>true</c>, related navigation properties will be included.</param>
	/// <returns>A task representing the asynchronous operation, containing the user if found; otherwise, <c>null</c>.</returns>
	Task<User?> GetUserByEmailAsync(string email, bool includeRelated = false);

	/// <summary>
	/// Retrieves a filtered list of users matching the specified predicate.
	/// </summary>
	/// <param name="predicate">The filter expression.</param>
	/// <returns>A task representing the asynchronous operation, containing the list of matching users.</returns>
	Task<List<User>> GetFilteredListAsync(Expression<Func<User, bool>> predicate);

	/// <summary>
	/// Retrieves a filtered and projected list of users matching the specified predicate.
	/// </summary>
	/// <typeparam name="T">The type to project the user entities to.</typeparam>
	/// <param name="predicate">The filter expression.</param>
	/// <param name="selector">The projection expression.</param>
	/// <returns>A task representing the asynchronous operation, containing the list of projected results.</returns>
	Task<List<T>> GetFilteredListProjectedToAsync<T>(Expression<Func<User, bool>> predicate, Expression<Func<User, T>> selector);

	/// <summary>
	/// Retrieves a filtered list of users based on optional dialect, Elo rating range, maximum workload, and limit.
	/// </summary>
	/// <param name="dialect">Optional dialect filter.</param>
	/// <param name="minElo">Optional minimum Elo rating.</param>
	/// <param name="maxElo">Optional maximum Elo rating.</param>
	/// <param name="maxWorkload">Optional maximum workload filter.</param>
	/// <param name="limit">Optional maximum number of users to retrieve.</param>
	/// <returns>A task representing the asynchronous operation, containing the filtered list of users.</returns>
	Task<List<User>> GetFilteredUser(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit);

	/// <summary>
	/// Retrieves all users, optionally including related navigation properties.
	/// </summary>
	/// <param name="includeRelated">If <c>true</c>, related entities will be included.</param>
	/// <returns>A task representing the asynchronous operation, containing the list of users.</returns>
	Task<List<User>> GetAllUsers(bool includeRelated = false);
}

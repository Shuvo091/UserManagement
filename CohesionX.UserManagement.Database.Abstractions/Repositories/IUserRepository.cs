// <copyright file="IUserRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Linq.Expressions;
using CohesionX.UserManagement.Database.Abstractions.Entities;

namespace CohesionX.UserManagement.Database.Abstractions.Repositories;

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
    /// Retrieves a user by their unique identifier asynchronously, with optional related data includes.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="trackChanges"> If <c>true</c>, tracks changes. </param>
    /// <param name="includeAll">
    /// If <c>true</c>, includes all related entities.
    /// </param>
    /// <param name="includes">
    /// Optional list of related navigation properties to include. Ignored if <paramref name="includeAll"/> is <c>true</c>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the user if found; otherwise, <c>null</c>.
    /// </returns>
    Task<User?> GetUserByIdAsync(
        Guid userId,
        bool trackChanges = false,
        bool includeAll = false,
        params Expression<Func<User, object>>[] includes);

    /// <summary>
    /// Retrieves a user by their email address asynchronously, with optional related data includes.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <param name="trackChanges"> If <c>true</c>, tracks changes. </param>
    /// <param name="includeAll">
    /// If <c>true</c>, includes all related entities.
    /// </param>
    /// <param name="includes">
    /// Optional list of related navigation properties to include. Ignored if <paramref name="includeAll"/> is <c>true</c>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the user if found; otherwise, <c>null</c>.
    /// </returns>
    Task<User?> GetUserByEmailAsync(
        string email,
        bool trackChanges = false,
        bool includeAll = false,
        params Expression<Func<User, object>>[] includes);

    /// <summary>
    /// Retrieves a filtered list of users matching the specified predicate asynchronously,
    /// with optional related data includes.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <param name="trackChanges"> If <c>true</c>, tracks changes. </param>
    /// <param name="includeAll">
    /// If <c>true</c>, includes all related entities.
    /// </param>
    /// <param name="includes">
    /// Optional list of related navigation properties to include. Ignored if <paramref name="includeAll"/> is <c>true</c>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the list of filtered users.
    /// </returns>
    Task<List<User>> GetFilteredListAsync(
        Expression<Func<User, bool>> predicate,
        bool trackChanges = false,
        bool includeAll = false,
        params Expression<Func<User, object>>[] includes);

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
}

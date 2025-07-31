// <copyright file="IUnitOfWork.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CohesionX.UserManagement.Database.Abstractions.Repositories;

/// <summary>
/// Defines a unit of work to group repository operations and commit them as a single transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Gets the repository for managing <c>EloHistory</c> entities.
    /// </summary>
    IEloRepository EloHistories { get; }

    /// <summary>
    /// Gets the repository for managing user statistics entities.
    /// </summary>
    IUserStatisticsRepository UserStatistics { get; }

    /// <summary>
    /// Gets the repository for managing user entities.
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Saves all pending changes across repositories to the data store asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, returning the number of affected records.</returns>
    Task<int> SaveChangesAsync();
}

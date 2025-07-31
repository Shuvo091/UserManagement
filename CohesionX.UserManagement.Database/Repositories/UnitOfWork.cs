// <copyright file="UnitOfWork.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CohesionX.UserManagement.Database.Abstractions.Repositories;

namespace CohesionX.UserManagement.Database.Repositories;

/// <summary>
/// Implements the Unit of Work pattern to coordinate multiple repository operations
/// and manage database transactions for the User Management module.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class
    /// with the specified application database context.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public UnitOfWork(AppDbContext context)
    {
        this.context = context;
        this.EloHistories = new EloRepository(this.context);
        this.UserStatistics = new UserStatisticsRepository(this.context);
        this.Users = new UserRepository(this.context);
    }

    /// <summary>
    /// Gets the repository for managing Elo history records.
    /// </summary>
    public IEloRepository EloHistories { get; }

    /// <summary>
    /// Gets the repository for managing user statistics entities.
    /// </summary>
    public IUserStatisticsRepository UserStatistics { get; }

    /// <summary>
    /// Gets the repository for managing user entities.
    /// </summary>
    public IUserRepository Users { get; }

    /// <summary>
    /// Persists all changes made in the context to the database asynchronously.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous save operation,
    /// returning the number of state entries written to the database.
    /// </returns>
    public async Task<int> SaveChangesAsync()
        => await this.context.SaveChangesAsync();
}

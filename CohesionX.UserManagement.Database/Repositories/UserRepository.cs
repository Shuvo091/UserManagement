// <copyright file="UserRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Linq.Expressions;
using CohesionX.UserManagement.Database.Abstractions.Entities;
using CohesionX.UserManagement.Database.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Database.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="User"/> entities.
/// Provides methods for querying users with optional eager loading of related entities.
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    private readonly AppDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class with the specified database context.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public UserRepository(AppDbContext context)
            : base(context)
    {
        this.context = context;
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(string email)
        => await this.context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant());

    /// <inheritdoc />
    public async Task<User?> GetUserByIdAsync(
        Guid userId,
        bool trackChanges = false,
        bool includeAll = false,
        params Expression<Func<User, object>>[] includes)
    {
        IQueryable<User> query = this.context.Users;

        if (trackChanges == false)
        {
            query = query.AsNoTracking();
        }

        if (includeAll)
        {
            query = query
                .Include(u => u.Dialects)
                .Include(u => u.Statistics)
                .Include(u => u.EloHistories)
                .Include(u => u.JobCompletions)
                .Include(u => u.JobClaims)
                .Include(u => u.AuditLogs)
                .Include(u => u.VerificationRecords);
        }
        else if (includes != null && includes.Length > 0)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }
        else
        {
            // Default minimal include
            query = query.Include(u => u.Statistics);
        }

        return await query.FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByEmailAsync(
        string email,
        bool trackChanges = false,
        bool includeAll = false,
        params Expression<Func<User, object>>[] includes)
    {
        IQueryable<User> query = this.context.Users;

        if (trackChanges == false)
        {
            query = query.AsNoTracking();
        }

        if (includeAll)
        {
            query = query
                .Include(u => u.Dialects)
                .Include(u => u.Statistics)
                .Include(u => u.EloHistories)
                .Include(u => u.JobCompletions)
                .Include(u => u.JobClaims)
                .Include(u => u.AuditLogs)
                .Include(u => u.VerificationRecords);
        }
        else if (includes != null && includes.Length > 0)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }
        else
        {
            // Default minimal include
            query = query.Include(u => u.Statistics);
        }

        return await query.FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <inheritdoc />
    public async Task<List<User>> GetFilteredListAsync(
        Expression<Func<User, bool>> predicate,
        bool trackChanges = false,
        bool includeAll = false,
        params Expression<Func<User, object>>[] includes)
    {
        IQueryable<User> query = this.context.Users;

        if (trackChanges == false)
        {
            query = query.AsNoTracking();
        }

        if (includeAll)
        {
            query = query
                .Include(u => u.Dialects)
                .Include(u => u.Statistics)
                .Include(u => u.EloHistories)
                .Include(u => u.JobCompletions)
                .Include(u => u.JobClaims)
                .Include(u => u.AuditLogs)
                .Include(u => u.VerificationRecords);
        }
        else if (includes != null && includes.Length > 0)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }
        else
        {
            // Default minimal include
            query = query.Include(u => u.Statistics);
        }

        return await query.Where(predicate).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<User>> GetFilteredUser(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit)
    {
        var query = this.context.Users
            .Include(u => u.Dialects)
            .Include(u => u.Statistics)
            .Include(u => u.JobClaims)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(dialect))
        {
            query = query.Where(u => u.Dialects.Any(d => d.Dialect == dialect));
        }

        if (minElo.HasValue)
        {
            query = query.Where(u => u.Statistics!.CurrentElo >= minElo.Value);
        }

        if (maxElo.HasValue)
        {
            query = query.Where(u => u.Statistics!.CurrentElo <= maxElo.Value);
        }

        if (maxWorkload.HasValue)
        {
            query = query.Where(u => u.JobClaims.Count <= maxWorkload.Value);
        }

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }
}

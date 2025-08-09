// <copyright file="JobCompletionRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CohesionX.UserManagement.Database.Abstractions.Entities;
using CohesionX.UserManagement.Database.Abstractions.Repositories;

namespace CohesionX.UserManagement.Database.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="JobCompletion"/> entities.
/// Inherits from the generic <see cref="Repository{T}"/> base class.
/// </summary>
public class JobCompletionRepository : Repository<JobCompletion>, IJobCompletionRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobCompletionRepository"/> class with the specified database context.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public JobCompletionRepository(AppDbContext context)
            : base(context)
    {
    }
}

// <copyright file="VerificationRequirementRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CohesionX.UserManagement.Database.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Database.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="UserVerificationRequirement"/> entities.
/// Provides methods to retrieve verification requirements configuration.
/// </summary>
public class VerificationRequirementRepository : Repository<UserVerificationRequirement>, IVerificationRequirementRepository
{
    private readonly AppDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="VerificationRequirementRepository"/> class
    /// with the specified database context.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public VerificationRequirementRepository(AppDbContext context)
            : base(context)
    {
        this.context = context;
    }

    /// <summary>
    /// Retrieves the first (and presumably global) user verification requirement configuration asynchronously.
    /// </summary>
    /// <param name="userId"> Id of user to get record. </param>
    /// <param name="trackChanges"> Whether or not to track changes. </param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the <see cref="UserVerificationRequirement"/> if found; otherwise, <c>null</c>.
    /// </returns>
    public async Task<UserVerificationRequirement?> GetVerificationRequirement(Guid userId, bool trackChanges = false)
    {
        if (trackChanges)
        {
            return await this.context.UserVerificationRequirements.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        return await this.context.UserVerificationRequirements.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId);
    }
}

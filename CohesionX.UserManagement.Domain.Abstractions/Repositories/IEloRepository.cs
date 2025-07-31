// <copyright file="IEloRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CohesionX.UserManagement.Database.Abstractions.Entities;

namespace CohesionX.UserManagement.Database.Abstractions.Repositories;

/// <summary>
/// Repository interface for managing <see cref="EloHistory"/> entities,
/// extending the generic <see cref="IRepository{T}"/> interface.
/// </summary>
public interface IEloRepository : IRepository<EloHistory>
{
    /// <summary>
    /// Retrieves all Elo history records associated with the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose Elo history is requested.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of <see cref="EloHistory"/> entries.</returns>
    Task<List<EloHistory>> GetByUserIdAsync(Guid userId);
}

// <copyright file="IJobCompletionRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CohesionX.UserManagement.Database.Abstractions.Entities;

namespace CohesionX.UserManagement.Database.Abstractions.Repositories;

/// <summary>
/// Repository interface for managing <see cref="JobCompletion"/> entities,
/// extending the generic <see cref="IRepository{T}"/> interface.
/// </summary>
public interface IJobCompletionRepository : IRepository<JobCompletion>
{
}

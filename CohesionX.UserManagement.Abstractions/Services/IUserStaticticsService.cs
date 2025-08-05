// <copyright file="IUserStaticticsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CohesionX.UserManagement.Database.Abstractions.Entities;
using SharedLibrary.Contracts.Usermanagement.Requests;

namespace CohesionX.UserManagement.Abstractions.Services;

/// <summary>
/// Provides operations for retrieving and updating user statistics.
/// </summary>
public interface IUserStaticticsService
{
    /// <summary>
    /// Gets the statistics for a specific user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>The user's statistics entity.</returns>
    UserStatistics GetUserStatisticsAsync(Guid userId);

    /// <summary>
    /// Updates the statistics for a specific user based on a recommended Elo change.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="userStatistics">The recommended Elo from QA to change data.</param>
    /// <returns>The updated user statistics entity.</returns>
    UserStatistics UpdateUserStatisticsAsync(Guid userId, RecommendedEloChangeDto userStatistics);
}

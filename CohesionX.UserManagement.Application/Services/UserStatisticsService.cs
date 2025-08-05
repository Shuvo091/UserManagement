// <copyright file="UserStatisticsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CohesionX.UserManagement.Abstractions.Services;
using CohesionX.UserManagement.Database.Abstractions.Entities;
using SharedLibrary.Contracts.Usermanagement.Requests;

namespace CohesionX.UserManagement.Application.Services;

/// <summary>
/// Provides operations for retrieving and updating user statistics.
/// </summary>
public class UserStatisticsService : IUserStaticticsService
{
    /// <summary>
    /// Gets the statistics for a specific user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>The user's statistics entity.</returns>
    public UserStatistics GetUserStatisticsAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Updates the statistics for a specific user based on a recommended Elo change.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="userStatistics">The recommended Elo change data.</param>
    /// <returns>The updated user statistics entity.</returns>
    public UserStatistics UpdateUserStatisticsAsync(Guid userId, RecommendedEloChangeDto userStatistics)
    {
        throw new NotImplementedException();
    }
}

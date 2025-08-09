// <copyright file="IAuditLogRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CohesionX.UserManagement.Database.Abstractions.Entities;
using SharedLibrary.Contracts.Usermanagement.RedisDtos;

namespace CohesionX.UserManagement.Database.Abstractions.Repositories;

/// <summary>
/// Defines data access methods for managing audit logs related to user activities.
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>
    /// Updates the audit log entry for a user's availability status,
    /// optionally including client IP address and user agent information.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose availability is being logged.</param>
    /// <param name="userAvailabilityRedis">The current availability state of the user retrieved from Redis cache.</param>
    /// <param name="ipAddress">The optional IP address from which the user action originated.</param>
    /// <param name="userAgent">The optional user agent string of the client device or browser.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AddAuditLog(
        Guid userId,
        UserAvailabilityRedisDto userAvailabilityRedis,
        string? ipAddress = null,
        string? userAgent = null);
}

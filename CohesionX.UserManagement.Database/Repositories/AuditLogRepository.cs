// <copyright file="AuditLogRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text.Json;
using CohesionX.UserManagement.Database.Abstractions.Entities;
using CohesionX.UserManagement.Database.Abstractions.Repositories;
using SharedLibrary.Contracts.Usermanagement.RedisDtos;

namespace CohesionX.UserManagement.Database.Repositories;

/// <summary>
/// Repository implementation for managing audit logs related to user activities.
/// </summary>
public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    private readonly AppDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogRepository"/> class with the specified database context.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public AuditLogRepository(AppDbContext context)
        : base(context)
    {
        this.context = context;
    }

    /// <summary>
    /// Creates a new audit log entry recording an update to a user's availability status.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose availability is being updated.</param>
    /// <param name="userAvailabilityRedis">The availability data retrieved from Redis, serialized to JSON for storage.</param>
    /// <param name="ipAddress">The optional IP address from which the update request originated.</param>
    /// <param name="userAgent">The optional user agent string of the client device or browser.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    public async Task AddAuditLog(
        Guid userId,
        UserAvailabilityRedisDto userAvailabilityRedis,
        string? ipAddress = null,
        string? userAgent = null)
    {
        await this.context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "UpdateAvailability",
            DetailsJson = JsonSerializer.Serialize(userAvailabilityRedis),
            IpAddress = ipAddress ?? string.Empty,
            UserAgent = userAgent ?? string.Empty,
            Timestamp = DateTime.UtcNow,
        });
    }
}

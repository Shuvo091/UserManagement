// <copyright file="RedisService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text.Json;
using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedLibrary.Cache.Services.Interfaces;
using SharedLibrary.Contracts.Usermanagement.RedisDtos;

namespace CohesionX.UserManagement.Application.Services;

/// <summary>
/// Provides operations for managing user availability, job claims, and Elo data in Redis.
/// </summary>
public class RedisService : IRedisService
{
    private readonly ICacheService cache;
    private readonly TimeSpan availabilityTtl;
    private readonly TimeSpan jobClaimLockTtl = TimeSpan.FromSeconds(30);
    private readonly TimeSpan userClaimsTtl = TimeSpan.FromHours(8);
    private readonly TimeSpan userEloTtl = TimeSpan.FromHours(1);
    private readonly ILogger<RedisService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisService"/> class.
    /// </summary>
    /// <param name="cache">The cache service for Redis operations.</param>
    /// <param name="appContantOptions"> app contants options. </param>
    /// <param name="logger"> logger. </param>
    public RedisService(ICacheService cache, IOptions<AppConstantsOptions> appContantOptions, ILogger<RedisService> logger)
    {
        this.cache = cache;

        // Read TTL in minutes from config, fallback to 360 minutes (6 hours) if missing or invalid
        var ttlMinutesStr = appContantOptions.Value.RedisCacheTtlMinutes;

        this.availabilityTtl = TimeSpan.FromMinutes(ttlMinutesStr);
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserAvailabilityRedisDto?> GetAvailabilityAsync(Guid userId)
    {
        var json = await this.cache.GetAsync<string>(this.GetAvailabilityKey(userId));
        if (string.IsNullOrWhiteSpace(json))
        {
            this.logger.LogInformation($"user {userId} not available in cache.");
            return null;
        }

        return JsonSerializer.Deserialize<UserAvailabilityRedisDto>(json);
    }

    /// <inheritdoc />
    public async Task SetAvailabilityAsync(Guid userId, UserAvailabilityRedisDto dto)
    {
        var json = JsonSerializer.Serialize(dto);
        await this.cache.SetAsync(this.GetAvailabilityKey(userId), json, this.availabilityTtl);
    }

    /// <inheritdoc />
    public async Task<bool> TryClaimJobAsync(string jobId, Guid userId)
    {
        var key = this.GetJobClaimLockKey(jobId);
        var existing = await this.cache.GetAsync<string>(key);
        if (!string.IsNullOrEmpty(existing))
        {
            this.logger.LogInformation($"Job {jobId} already claimed.");
            return false;
        }

        await this.cache.SetAsync(key, $"{userId}_claiming_{jobId}", this.jobClaimLockTtl);
        return true;
    }

    /// <inheritdoc />
    public async Task ReleaseJobClaimAsync(string jobId)
    {
        await this.cache.RemoveAsync(this.GetJobClaimLockKey(jobId));
    }

    /// <inheritdoc />
    public async Task<List<string>> GetUserClaimsAsync(Guid userId)
    {
        var json = await this.cache.GetAsync<string>(this.GetUserClaimsKey(userId));
        if (string.IsNullOrWhiteSpace(json))
        {
            this.logger.LogInformation($"user {userId}'s claim not found.");
            return new List<string>();
        }

        var jobs = JsonSerializer.Deserialize<List<string>>(json);
        return jobs ?? new List<string>();
    }

    /// <inheritdoc />
    public async Task AddUserClaimAsync(Guid userId, string jobId)
    {
        var key = this.GetUserClaimsKey(userId);
        var existing = await this.GetUserClaimsAsync(userId);
        if (!existing.Contains(jobId))
        {
            existing.Add(jobId);
        }

        var json = JsonSerializer.Serialize(existing);
        await this.cache.SetAsync(key, json, this.userClaimsTtl);
    }

    /// <inheritdoc />
    public async Task RemoveUserClaimAsync(Guid userId, string jobId)
    {
        var key = this.GetUserClaimsKey(userId);
        var existing = await this.GetUserClaimsAsync(userId);
        if (existing.Contains(jobId))
        {
            existing.Remove(jobId);
            var json = JsonSerializer.Serialize(existing);
            await this.cache.SetAsync(key, json, this.userClaimsTtl);
        }
    }

    /// <inheritdoc />
    public async Task<UserEloRedisDto?> GetUserEloAsync(Guid userId)
    {
        var json = await this.cache.GetAsync<string>(this.GetUserEloKey(userId));
        return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<UserEloRedisDto>(json);
    }

    /// <inheritdoc />
    public async Task SetUserEloAsync(Guid userId, UserEloRedisDto dto)
    {
        var json = JsonSerializer.Serialize(dto);
        await this.cache.SetAsync(this.GetUserEloKey(userId), json, this.userEloTtl);
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, UserAvailabilityRedisDto>> GetBulkAvailabilityAsync(IEnumerable<Guid> userIds)
    {
        var idList = userIds.ToList();
        var availabilityKeys = idList.Select(id => (object)this.GetAvailabilityKey(id)).ToArray();

        // Start all Redis fetches
        var availabilityTasks = availabilityKeys.Select(k => this.cache.GetAsync<string>((string)k)).ToList();

        await Task.WhenAll(availabilityTasks);

        var availabilityMap = new Dictionary<Guid, UserAvailabilityRedisDto>();

        for (int i = 0; i < idList.Count; i++)
        {
            var userId = idList[i];

            // Availability
            var availabilityJson = availabilityTasks[i].Result;
            if (!string.IsNullOrWhiteSpace(availabilityJson))
            {
                var availability = JsonSerializer.Deserialize<UserAvailabilityRedisDto>(availabilityJson);
                if (availability != null)
                {
                    availabilityMap[userId] = availability;
                }
            }
        }

        return availabilityMap;
    }

    /// <summary>
    /// Gets the cache key for user availability.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>The cache key string.</returns>
    private string GetAvailabilityKey(Guid userId) => $"user:availability:{userId}";

    /// <summary>
    /// Gets the cache key for job claim lock.
    /// </summary>
    /// <param name="jobId">The job's unique identifier.</param>
    /// <returns>The cache key string.</returns>
    private string GetJobClaimLockKey(string jobId) => $"job:claim:lock:{jobId}";

    /// <summary>
    /// Gets the cache key for user claims.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>The cache key string.</returns>
    private string GetUserClaimsKey(Guid userId) => $"user:claims:{userId}";

    /// <summary>
    /// Gets the cache key for user Elo data.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>The cache key string.</returns>
    private string GetUserEloKey(Guid userId) => $"user:elo:{userId}";
}

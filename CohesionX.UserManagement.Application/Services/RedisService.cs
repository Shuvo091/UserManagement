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
        if (userId == Guid.Empty)
        {
            this.logger.LogError("UserId cannot be empty. UserId: {UserId}", userId);
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        try
        {
            var json = await this.cache.GetAsync<string>(this.GetAvailabilityKey(userId));
            if (string.IsNullOrWhiteSpace(json))
            {
                this.logger.LogInformation("User availability not found in cache. UserId: {UserId}", userId);
                return null;
            }
            else
            {
                this.logger.LogDebug("User availability retrieved from cache. UserId: {UserId}", userId);
                return JsonSerializer.Deserialize<UserAvailabilityRedisDto>(json);
            }
        }
        catch (JsonException ex)
        {
            this.logger.LogError(ex, "Failed to deserialize user availability. UserId: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to get user availability. UserId: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SetAvailabilityAsync(Guid userId, UserAvailabilityRedisDto dto)
    {
        if (userId == Guid.Empty)
        {
            this.logger.LogError("UserId cannot be empty. UserId: {UserId}", userId);
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        if (dto == null)
        {
            this.logger.LogError("DTO cannot be null. UserId: {UserId}", userId);
            throw new ArgumentNullException(nameof(dto));
        }

        try
        {
            var json = JsonSerializer.Serialize(dto);
            await this.cache.SetAsync(this.GetAvailabilityKey(userId), json, this.availabilityTtl);
            this.logger.LogInformation("User availability set in cache. UserId: {UserId}, TTLMinutes: {TTL}", userId, this.availabilityTtl.TotalMinutes);
        }
        catch (JsonException ex)
        {
            this.logger.LogError(ex, "Failed to serialize user availability. UserId: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to set user availability. UserId: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryClaimJobAsync(string jobId, Guid userId)
    {
        if (string.IsNullOrEmpty(jobId))
        {
            this.logger.LogError("JobId cannot be null or empty. JobId: {JobId}", jobId);
            throw new ArgumentException("JobId cannot be null or empty.", nameof(jobId));
        }

        if (userId == Guid.Empty)
        {
            this.logger.LogError("UserId cannot be empty. UserId: {UserId}", userId);
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        try
        {
            var key = this.GetJobClaimLockKey(jobId);
            var existing = await this.cache.GetAsync<string>(key);
            if (!string.IsNullOrEmpty(existing))
            {
                this.logger.LogInformation("Job already claimed. JobId: {JobId}, ExistingClaim: {Existing}", jobId, existing);
                return false;
            }
            else
            {
                await this.cache.SetAsync(key, $"{userId}_claiming_{jobId}", this.jobClaimLockTtl);
                this.logger.LogInformation("Job claim set. JobId: {JobId}, UserId: {UserId}, TTLMinutes: {TTL}", jobId, userId, this.jobClaimLockTtl.TotalMinutes);
                return true;
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to claim job. JobId: {JobId}, UserId: {UserId}", jobId, userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ReleaseJobClaimAsync(string jobId)
    {
        if (string.IsNullOrEmpty(jobId))
        {
            this.logger.LogError("JobId cannot be null or empty. JobId: {JobId}", jobId);
            throw new ArgumentException("JobId cannot be null or empty.", nameof(jobId));
        }

        try
        {
            await this.cache.RemoveAsync(this.GetJobClaimLockKey(jobId));
            this.logger.LogInformation("Job claim released. JobId: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to release job claim. JobId: {JobId}", jobId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<string>> GetUserClaimsAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            this.logger.LogError("UserId cannot be empty. UserId: {UserId}", userId);
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        try
        {
            var json = await this.cache.GetAsync<string>(this.GetUserClaimsKey(userId));
            if (string.IsNullOrWhiteSpace(json))
            {
                this.logger.LogInformation("No user claims found in cache. UserId: {UserId}", userId);
                return new List<string>();
            }
            else
            {
                var jobs = JsonSerializer.Deserialize<List<string>>(json);
                this.logger.LogDebug("User claims retrieved from cache. UserId: {UserId}, ClaimsCount: {Count}", userId, jobs?.Count ?? 0);
                return jobs ?? new List<string>();
            }
        }
        catch (JsonException ex)
        {
            this.logger.LogError(ex, "Failed to deserialize user claims. UserId: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to get user claims. UserId: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task AddUserClaimAsync(Guid userId, string jobId)
    {
        if (userId == Guid.Empty)
        {
            this.logger.LogError("UserId cannot be empty. UserId: {UserId}", userId);
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        if (string.IsNullOrEmpty(jobId))
        {
            this.logger.LogError("JobId cannot be null or empty. JobId: {JobId}", jobId);
            throw new ArgumentException("JobId cannot be null or empty.", nameof(jobId));
        }

        try
        {
            var key = this.GetUserClaimsKey(userId);
            var existing = await this.GetUserClaimsAsync(userId);
            if (!existing.Contains(jobId))
            {
                existing.Add(jobId);
            }

            var json = JsonSerializer.Serialize(existing);
            await this.cache.SetAsync(key, json, this.userClaimsTtl);
            this.logger.LogInformation("User claim added to cache. UserId: {UserId}, JobId: {JobId}, TotalClaims: {Count}, TTLMinutes: {TTL}", userId, jobId, existing.Count, this.userClaimsTtl.TotalMinutes);
        }
        catch (JsonException ex)
        {
            this.logger.LogError(ex, "Failed to serialize user claims. UserId: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to add user claim. UserId: {UserId}, JobId: {JobId}", userId, jobId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveUserClaimAsync(Guid userId, string jobId)
    {
        if (userId == Guid.Empty)
        {
            this.logger.LogError("UserId cannot be empty. UserId: {UserId}", userId);
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        if (string.IsNullOrEmpty(jobId))
        {
            this.logger.LogError("JobId cannot be null or empty. JobId: {JobId}", jobId);
            throw new ArgumentException("JobId cannot be null or empty.", nameof(jobId));
        }

        try
        {
            var key = this.GetUserClaimsKey(userId);
            var existing = await this.GetUserClaimsAsync(userId);
            if (existing.Contains(jobId))
            {
                existing.Remove(jobId);
                var json = JsonSerializer.Serialize(existing);
                await this.cache.SetAsync(key, json, this.userClaimsTtl);
                this.logger.LogInformation("User claim removed from cache. UserId: {UserId}, JobId: {JobId}, RemainingClaims: {Count}, TTLMinutes: {TTL}", userId, jobId, existing.Count, this.userClaimsTtl.TotalMinutes);
            }
            else
            {
                this.logger.LogInformation("User claim not found for removal. UserId: {UserId}, JobId: {JobId}", userId, jobId);
            }
        }
        catch (JsonException ex)
        {
            this.logger.LogError(ex, "Failed to serialize user claims during removal. UserId: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to remove user claim. UserId: {UserId}, JobId: {JobId}", userId, jobId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<UserEloRedisDto?> GetUserEloAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            this.logger.LogError("UserId cannot be empty. UserId: {UserId}", userId);
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        try
        {
            var json = await this.cache.GetAsync<string>(this.GetUserEloKey(userId));
            if (string.IsNullOrWhiteSpace(json))
            {
                this.logger.LogInformation("User Elo not found in cache. UserId: {UserId}", userId);
                return null;
            }
            else
            {
                var elo = JsonSerializer.Deserialize<UserEloRedisDto>(json);
                this.logger.LogDebug("User Elo retrieved from cache. UserId: {UserId}, CurrentElo: {CurrentElo}", userId, elo?.CurrentElo);
                return elo;
            }
        }
        catch (JsonException ex)
        {
            this.logger.LogError(ex, "Failed to deserialize user Elo. UserId: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to get user Elo. UserId: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SetUserEloAsync(Guid userId, UserEloRedisDto dto)
    {
        if (userId == Guid.Empty)
        {
            this.logger.LogError("UserId cannot be empty. UserId: {UserId}", userId);
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        if (dto == null)
        {
            this.logger.LogError("UserEloRedisDto cannot be null. UserId: {UserId}", userId);
            throw new ArgumentNullException(nameof(dto));
        }

        try
        {
            var json = JsonSerializer.Serialize(dto);
            await this.cache.SetAsync(this.GetUserEloKey(userId), json, this.userEloTtl);
            this.logger.LogInformation("User Elo set in cache. UserId: {UserId}, CurrentElo: {CurrentElo}, PeakElo: {PeakElo}, GamesPlayed: {GamesPlayed}, TTLMinutes: {TTL}", userId, dto.CurrentElo, dto.PeakElo, dto.GamesPlayed, this.userEloTtl.TotalMinutes);
        }
        catch (JsonException ex)
        {
            this.logger.LogError(ex, "Failed to serialize user Elo. UserId: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to set user Elo in cache. UserId: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, UserAvailabilityRedisDto>> GetBulkAvailabilityAsync(IEnumerable<Guid> userIds)
    {
        if (userIds == null)
        {
            this.logger.LogError("UserIds collection cannot be null.");
            throw new ArgumentNullException(nameof(userIds));
        }

        try
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

            this.logger.LogInformation("Bulk availability fetched from cache. UserCount: {Count}, RetrievedCount: {RetrievedCount}", idList.Count, availabilityMap.Count);
            return availabilityMap;
        }
        catch (JsonException ex)
        {
            this.logger.LogError(ex, "Failed to deserialize bulk user availability.");
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to fetch bulk user availability.");
            throw;
        }
    }

    private string GetAvailabilityKey(Guid userId) => $"user:availability:{userId}";

    private string GetJobClaimLockKey(string jobId) => $"job:claim:lock:{jobId}";

    private string GetUserClaimsKey(Guid userId) => $"user:claims:{userId}";

    private string GetUserEloKey(Guid userId) => $"user:elo:{userId}";
}

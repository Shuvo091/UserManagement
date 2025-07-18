using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace CohesionX.UserManagement.Modules.Users.Application.Services;

public class RedisService : IRedisService
{
	private readonly IDistributedCache _cache;

	public RedisService(IDistributedCache cache)
	{
		_cache = cache;
	}

	private static DistributedCacheEntryOptions GetTtlOptions(TimeSpan? ttl)
	{
		return new DistributedCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromHours(6)
		};
	}

	private string GetAvailabilityKey(Guid userId) => $"user:availability:{userId}";

	public async Task<UserAvailabilityDto?> GetAvailabilityAsync(Guid userId)
	{
		var json = await _cache.GetStringAsync(GetAvailabilityKey(userId));
		if (string.IsNullOrWhiteSpace(json)) return null;
		return JsonSerializer.Deserialize<UserAvailabilityDto>(json);
	}

	public async Task SetAvailabilityAsync(Guid userId, UserAvailabilityDto dto, TimeSpan? ttl = null)
	{
		var json = JsonSerializer.Serialize(dto);
		await _cache.SetStringAsync(GetAvailabilityKey(userId), json, GetTtlOptions(ttl));
	}

	public async Task<bool> TryClaimJobAsync(Guid jobId, Guid userId)
	{
		var key = $"job:claim:lock:{jobId}";
		var existing = await _cache.GetStringAsync(key);
		if (!string.IsNullOrEmpty(existing)) return false;
		await _cache.SetStringAsync(key, userId.ToString(), GetTtlOptions(TimeSpan.FromSeconds(30)));
		return true;
	}

	public async Task ReleaseJobClaimAsync(Guid jobId)
	{
		await _cache.RemoveAsync($"job:claim:lock:{jobId}");
	}

	public async Task<List<Guid>> GetUserClaimsAsync(Guid userId)
	{
		var key = $"user:claims:{userId}";
		var json = await _cache.GetStringAsync(key);
		return string.IsNullOrWhiteSpace(json) ? new List<Guid>() : JsonSerializer.Deserialize<List<Guid>>(json)!;
	}

	public async Task AddUserClaimAsync(Guid userId, Guid jobId, TimeSpan? ttl = null)
	{
		var key = $"user:claims:{userId}";
		var existing = await GetUserClaimsAsync(userId);
		if (!existing.Contains(jobId)) existing.Add(jobId);
		var json = JsonSerializer.Serialize(existing);
		await _cache.SetStringAsync(key, json, GetTtlOptions(ttl ?? TimeSpan.FromHours(8)));
	}

	public async Task RemoveUserClaimAsync(Guid userId, Guid jobId)
	{
		var key = $"user:claims:{userId}";
		var existing = await GetUserClaimsAsync(userId);
		if (existing.Contains(jobId))
		{
			existing.Remove(jobId);
			var json = JsonSerializer.Serialize(existing);
			await _cache.SetStringAsync(key, json, GetTtlOptions(TimeSpan.FromHours(8)));
		}
	}

	public async Task<UserEloDto?> GetUserEloAsync(Guid userId)
	{
		var key = $"user:elo:{userId}";
		var json = await _cache.GetStringAsync(key);
		return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<UserEloDto>(json);
	}

	public async Task SetUserEloAsync(Guid userId, UserEloDto dto, TimeSpan? ttl = null)
	{
		var key = $"user:elo:{userId}";
		var json = JsonSerializer.Serialize(dto);
		await _cache.SetStringAsync(key, json, GetTtlOptions(ttl ?? TimeSpan.FromHours(1)));
	}
}

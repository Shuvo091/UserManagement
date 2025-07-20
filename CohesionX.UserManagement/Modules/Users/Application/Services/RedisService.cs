using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using SharedLibrary.Cache.Services.Interfaces;
using System.Text.Json;

namespace CohesionX.UserManagement.Modules.Users.Application.Services;

public class RedisService : IRedisService
{
	private readonly ICacheService _cache;
	private readonly TimeSpan _availabilityTtl;
	private readonly TimeSpan _jobClaimLockTtl = TimeSpan.FromSeconds(30);
	private readonly TimeSpan _userClaimsTtl = TimeSpan.FromHours(8);
	private readonly TimeSpan _userEloTtl = TimeSpan.FromHours(1);

	public RedisService(ICacheService cache, IConfiguration configuration)
	{
		_cache = cache;

		// Read TTL in minutes from config, fallback to 360 minutes (6 hours) if missing or invalid
		var ttlMinutesStr = configuration["REDIS_CACHE_TTL_MINUTES"];
		if (!int.TryParse(ttlMinutesStr, out var ttlMinutes))
			ttlMinutes = 360;

		_availabilityTtl = TimeSpan.FromMinutes(ttlMinutes);
	}

	private string GetAvailabilityKey(Guid userId) => $"user:availability:{userId}";
	private string GetJobClaimLockKey(string jobId) => $"job:claim:lock:{jobId}";
	private string GetUserClaimsKey(Guid userId) => $"user:claims:{userId}";
	private string GetUserEloKey(Guid userId) => $"user:elo:{userId}";

	public async Task<UserAvailabilityRedisDto?> GetAvailabilityAsync(Guid userId)
	{
		var json = await _cache.GetAsync<string>(GetAvailabilityKey(userId));
		if (string.IsNullOrWhiteSpace(json)) return null;
		return JsonSerializer.Deserialize<UserAvailabilityRedisDto>(json);
	}

	public async Task SetAvailabilityAsync(Guid userId, UserAvailabilityRedisDto dto)
	{
		var json = JsonSerializer.Serialize(dto);
		await _cache.SetAsync<string>(GetAvailabilityKey(userId), json, _availabilityTtl);
	}

	public async Task<bool> TryClaimJobAsync(string jobId, Guid userId)
	{
		var key = GetJobClaimLockKey(jobId);
		var existing = await _cache.GetAsync<string>(key);
		if (!string.IsNullOrEmpty(existing)) return false;
		await _cache.SetAsync(key, $"{userId}_claiming_{jobId}", _jobClaimLockTtl);
		return true;
	}

	public async Task ReleaseJobClaimAsync(string jobId)
	{
		await _cache.RemoveAsync(GetJobClaimLockKey(jobId));
	}

	public async Task<List<string>> GetUserClaimsAsync(Guid userId)
	{
		var json = await _cache.GetAsync<string>(GetUserClaimsKey(userId));
		if (string.IsNullOrWhiteSpace(json)) return new List<string>();
		var jobs = JsonSerializer.Deserialize<List<string>>(json);
		return jobs ?? new List<string>();
	}

	public async Task AddUserClaimAsync(Guid userId, string jobId)
	{
		var key = GetUserClaimsKey(userId);
		var existing = await GetUserClaimsAsync(userId);
		if (!existing.Contains(jobId)) existing.Add(jobId);
		var json = JsonSerializer.Serialize(existing);
		await _cache.SetAsync<string>(key, json, _userClaimsTtl);
	}

	public async Task RemoveUserClaimAsync(Guid userId, string jobId)
	{
		var key = GetUserClaimsKey(userId);
		var existing = await GetUserClaimsAsync(userId);
		if (existing.Contains(jobId))
		{
			existing.Remove(jobId);
			var json = JsonSerializer.Serialize(existing);
			await _cache.SetAsync<string>(key, json, _userClaimsTtl);
		}
	}

	public async Task<UserEloDto?> GetUserEloAsync(Guid userId)
	{
		var json = await _cache.GetAsync<string>(GetUserEloKey(userId));
		return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<UserEloDto>(json);
	}

	public async Task SetUserEloAsync(Guid userId, UserEloDto dto)
	{
		var json = JsonSerializer.Serialize(dto);
		await _cache.SetAsync<string>(GetUserEloKey(userId), json, _userEloTtl);
	}

	public async Task<Dictionary<Guid, UserAvailabilityRedisDto>> GetBulkAvailabilityAsync(IEnumerable<Guid> userIds)
	{
		var idList = userIds.ToList();
		var availabilityKeys = idList.Select(id => (object)GetAvailabilityKey(id)).ToArray();

		// Start all Redis fetches
		var availabilityTasks = availabilityKeys.Select(k => _cache.GetAsync<string>((string)k)).ToList();

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
					availabilityMap[userId] = availability;
			}
		}

		return availabilityMap;
	}

}

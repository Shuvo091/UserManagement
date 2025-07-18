using CohesionX.UserManagement.Modules.Users.Application.DTOs;

namespace CohesionX.UserManagement.Modules.Users.Application.Interfaces;

public interface IRedisService
{
	Task<UserAvailabilityRedisDto?> GetAvailabilityAsync(Guid userId);
	Task SetAvailabilityAsync(Guid userId, UserAvailabilityRedisDto dto);

	Task<bool> TryClaimJobAsync(string jobId, Guid userId);
	Task ReleaseJobClaimAsync(string jobId);

	Task<List<string>> GetUserClaimsAsync(Guid userId);
	Task AddUserClaimAsync(Guid userId, string jobId);
	Task RemoveUserClaimAsync(Guid userId, string jobId);

	Task<UserEloDto?> GetUserEloAsync(Guid userId);
	Task SetUserEloAsync(Guid userId, UserEloDto dto);

	Task<(Dictionary<Guid, UserAvailabilityRedisDto> AvailabilityMap, Dictionary<Guid, UserEloDto> EloMap)> GetBulkAvailabilityAndEloAsync(IEnumerable<Guid> userIds);
}

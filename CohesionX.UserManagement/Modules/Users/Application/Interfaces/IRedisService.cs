using CohesionX.UserManagement.Modules.Users.Application.DTOs;

namespace CohesionX.UserManagement.Modules.Users.Application.Interfaces;

public interface IRedisService
{
	Task<UserAvailabilityDto?> GetAvailabilityAsync(Guid userId);
	Task SetAvailabilityAsync(Guid userId, UserAvailabilityDto dto, TimeSpan? ttl = null);

	Task<bool> TryClaimJobAsync(Guid jobId, Guid userId);
	Task ReleaseJobClaimAsync(Guid jobId);

	Task<List<Guid>> GetUserClaimsAsync(Guid userId);
	Task AddUserClaimAsync(Guid userId, Guid jobId, TimeSpan? ttl = null);
	Task RemoveUserClaimAsync(Guid userId, Guid jobId);

	Task<UserEloDto?> GetUserEloAsync(Guid userId);
	Task SetUserEloAsync(Guid userId, UserEloDto dto, TimeSpan? ttl = null);
}

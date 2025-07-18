using CohesionX.UserManagement.Modules.Users.Application.DTOs;

namespace CohesionX.UserManagement.Modules.Users.Application.Interfaces;

public interface IRedisService
{
	Task<UserAvailabilityDto?> GetAvailabilityAsync(Guid userId);
	Task SetAvailabilityAsync(Guid userId, UserAvailabilityDto dto);

	Task<bool> TryClaimJobAsync(string jobId, Guid userId);
	Task ReleaseJobClaimAsync(string jobId);

	Task<List<string>> GetUserClaimsAsync(Guid userId);
	Task AddUserClaimAsync(Guid userId, string jobId);
	Task RemoveUserClaimAsync(Guid userId, string jobId);

	Task<UserEloDto?> GetUserEloAsync(Guid userId);
	Task SetUserEloAsync(Guid userId, UserEloDto dto);
}

using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Application.Interfaces;

public interface IRedisService
{
	Task<UserAvailabilityRedisDto?> GetAvailabilityAsync(Guid userId);
	Task SetAvailabilityAsync(Guid userId, UserAvailabilityRedisDto dto);

	Task<bool> TryClaimJobAsync(string jobId, Guid userId);
	Task ReleaseJobClaimAsync(string jobId);

	Task<List<string>> GetUserClaimsAsync(Guid userId);
	Task AddUserClaimAsync(Guid userId, string jobId);
	Task RemoveUserClaimAsync(Guid userId, string jobId);

	Task<UserEloRedisDto?> GetUserEloAsync(Guid userId);
	Task SetUserEloAsync(Guid userId, UserEloRedisDto dto);

	Task<Dictionary<Guid, UserAvailabilityRedisDto>> GetBulkAvailabilityAsync(IEnumerable<Guid> userIds);
}

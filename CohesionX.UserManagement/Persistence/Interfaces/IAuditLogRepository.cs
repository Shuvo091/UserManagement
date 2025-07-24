using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Persistence.Interfaces;

public interface IAuditLogRepository
{
	Task UpdateUserAvailabilityAuditLog(Guid userId, UserAvailabilityRedisDto userAvailabilityRedis, string? ipAddress = null, string? userAgent = null);
}
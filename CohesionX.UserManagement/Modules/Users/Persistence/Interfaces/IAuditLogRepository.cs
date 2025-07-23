using SharedLibrary.RequestResponseModels.UserManagement;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;

namespace CohesionX.UserManagement.Modules.Users.Persistence.Interfaces;

public interface IAuditLogRepository
{
	Task UpdateUserAvailabilityAuditLog(Guid userId, UserAvailabilityRedisDto userAvailabilityRedis, string? ipAddress = null, string? userAgent = null);
}
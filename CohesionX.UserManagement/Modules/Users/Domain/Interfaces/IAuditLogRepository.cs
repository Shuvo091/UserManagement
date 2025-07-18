using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;

namespace CohesionX.UserManagement.Modules.Users.Domain.Interfaces;

public interface IAuditLogRepository
{
	Task UpdateUserAvailabilityAuditLog(Guid userId, UserAvailabilityRedisDto userAvailabilityRedis, string? ipAddress = null, string? userAgent = null);
}
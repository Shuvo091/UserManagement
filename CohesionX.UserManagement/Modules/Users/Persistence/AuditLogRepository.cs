using SharedLibrary.RequestResponseModels.UserManagement;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Persistence.Interfaces;
using CohesionX.UserManagement.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CohesionX.UserManagement.Modules.Users.Persistence;

public class AuditLogRepository : IAuditLogRepository
{
	private readonly AppDbContext _db;

	public AuditLogRepository(AppDbContext db)
	{
		_db = db;
	}

	public async Task UpdateUserAvailabilityAuditLog(Guid userId, UserAvailabilityRedisDto userAvailabilityRedis, string? ipAddress = null, string? userAgent = null)
	{
		_db.AuditLogs.Add(new AuditLog
		{
			UserId = userId,
			Action = "UpdateAvailability",
			DetailsJson = JsonSerializer.Serialize(userAvailabilityRedis),
			IpAddress = ipAddress ?? string.Empty,
			UserAgent = userAgent ?? string.Empty,
			Timestamp = DateTime.UtcNow
		});

		await _db.SaveChangesAsync();
	}

}

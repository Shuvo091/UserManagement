using SharedLibrary.RequestResponseModels.UserManagement;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using CohesionX.UserManagement.Persistence.Interfaces;
using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.UserManagement.Persistence;

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

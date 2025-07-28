using System.Text.Json;
using CohesionX.UserManagement.Domain.Entities;
using CohesionX.UserManagement.Persistence.Interfaces;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Persistence;

/// <summary>
/// Repository implementation for managing audit logs related to user activities.
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
	private readonly AppDbContext _db;

	/// <summary>
	/// Initializes a new instance of the <see cref="AuditLogRepository"/> class with the specified database context.
	/// </summary>
	/// <param name="db">The application database context.</param>
	public AuditLogRepository(AppDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// Creates a new audit log entry recording an update to a user's availability status.
	/// </summary>
	/// <param name="userId">The unique identifier of the user whose availability is being updated.</param>
	/// <param name="userAvailabilityRedis">The availability data retrieved from Redis, serialized to JSON for storage.</param>
	/// <param name="ipAddress">The optional IP address from which the update request originated.</param>
	/// <param name="userAgent">The optional user agent string of the client device or browser.</param>
	/// <returns>A task representing the asynchronous save operation.</returns>
	public async Task UpdateUserAvailabilityAuditLog(
		Guid userId,
		UserAvailabilityRedisDto userAvailabilityRedis,
		string? ipAddress = null,
		string? userAgent = null)
	{
		_db.AuditLogs.Add(new AuditLog
		{
			UserId = userId,
			Action = "UpdateAvailability",
			DetailsJson = JsonSerializer.Serialize(userAvailabilityRedis),
			IpAddress = ipAddress ?? string.Empty,
			UserAgent = userAgent ?? string.Empty,
			Timestamp = DateTime.UtcNow,
		});

		await _db.SaveChangesAsync();
	}
}

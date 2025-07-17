namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class AuditLogDto
{
	public string Action { get; set; } = default!;
	public string DetailsJson { get; set; } = default!;
	public string IpAddress { get; set; } = default!;
	public string UserAgent { get; set; } = default!;
	public DateTime Timestamp { get; set; }
}
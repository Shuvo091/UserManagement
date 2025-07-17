namespace CohesionX.UserManagement.Modules.Users.Domain.Entities;
public class AuditLog
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public string Action { get; set; } = default!;
	public string DetailsJson { get; set; } = default!;
	public string IpAddress { get; set; } = default!;
	public string UserAgent { get; set; } = default!;
	public DateTime Timestamp { get; set; }

	public User User { get; set; } = default!;
}

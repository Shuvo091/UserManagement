namespace CohesionX.UserManagement.Domain.Entities;
public class AuditLog : BaseEntity
{
	public Guid UserId { get; set; }
	public string Action { get; set; } = default!;
	public string DetailsJson { get; set; } = default!;
	public string IpAddress { get; set; } = default!;
	public string UserAgent { get; set; } = default!;
	public DateTime Timestamp { get; set; }

	public User User { get; set; } = default!;
}

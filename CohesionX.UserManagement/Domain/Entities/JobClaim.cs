namespace CohesionX.UserManagement.Domain.Entities;

public class JobClaim : BaseEntity
{
	public Guid UserId { get; set; }
	public string JobId { get; set; } = default!;
	public DateTime ClaimedAt { get; set; }
	public DateTime BookOutExpiresAt { get; set; }
	public string Status { get; set; } = default!;
	public DateTime CreatedAt { get; set; }

	public User User { get; set; } = default!;
}

namespace CohesionX.UserManagement.Modules.Users.Domain.Entities;

public class JobClaim
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public string JobId { get; set; } = default!;
	public DateTime ClaimedAt { get; set; }
	public DateTime BookOutExpiresAt { get; set; }
	public string Status { get; set; } = default!;
	public DateTime CreatedAt { get; set; }

	public User User { get; set; } = default!;
}

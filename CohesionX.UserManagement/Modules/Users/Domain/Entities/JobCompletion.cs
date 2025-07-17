namespace CohesionX.UserManagement.Modules.Users.Domain.Entities;

public class JobCompletion
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public string JobId { get; set; } = default!;
	public string Outcome { get; set; } = default!;
	public int EloChange { get; set; }
	public string CompletionId { get; set; } = default!;
	public DateTime CompletedAt { get; set; }
	public DateTime CreatedAt { get; set; }

	public User User { get; set; } = default!;
}

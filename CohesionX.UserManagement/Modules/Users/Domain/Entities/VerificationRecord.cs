namespace CohesionX.UserManagement.Modules.Users.Domain.Entities;

public class VerificationRecord
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public string VerificationType { get; set; } = default!;
	public string Status { get; set; } = default!;
	public string VerificationLevel { get; set; } = default!;
	public string VerificationData { get; set; } = default!;
	public DateTime? VerifiedAt { get; set; }
	public DateTime CreatedAt { get; set; }

	public User User { get; set; } = default!;
}

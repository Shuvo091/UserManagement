namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class JobClaimDto
{
	public string JobId { get; set; } = default!;
	public DateTime ClaimedAt { get; set; }
	public DateTime BookOutExpiresAt { get; set; }
	public string Status { get; set; } = default!;
}
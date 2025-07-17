namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;


public class VerificationRecordDto
{
	public string VerificationType { get; set; } = default!;
	public string Status { get; set; } = default!;
	public string VerificationLevel { get; set; } = default!;
	public string VerificationData { get; set; } = default!;
	public DateTime? VerifiedAt { get; set; }
}

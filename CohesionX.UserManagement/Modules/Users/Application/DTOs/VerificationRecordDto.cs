namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;


public class VerificationRecordDto
{
	public string VerificationType { get; set; } = default!;
	public string Status { get; set; } = default!;
	public string VerificationLevel { get; set; } = default!;
	public string VerificationData { get; set; } = default!;
	public DateTime? VerifiedAt { get; set; }
	public DateTime? CreatedAt { get; set; }
}

public class VerificationRequest
{
	public string VerificationType { get; set; }
	public IdDocumentValidationDto IdDocumentValidation { get; set; }
	public AdditionalVerificationDto AdditionalVerification { get; set; }
}

public class IdDocumentValidationDto
{
	public bool Enabled { get; set; }
	public string IdNumber { get; set; }
	public bool PhotoUploaded { get; set; }
	public ValidationResultDto ValidationResult { get; set; }
}

public class ValidationResultDto
{
	public bool IdFormatValid { get; set; }
	public bool PhotoPresent { get; set; }
	public string Note { get; set; }
}

public class AdditionalVerificationDto
{
	public bool PhoneVerification { get; set; }
	public bool EmailVerification { get; set; }
}


public class VerificationResponse
{
	public string VerificationStatus { get; set; }
	public int EloRating { get; set; }
	public string StatusChanged { get; set; }
	public bool EligibleForWork { get; set; }
	public string ActivationMethod { get; set; }
	public DateTime ActivatedAt { get; set; }
	public string VerificationLevel { get; set; }
	public string[] NextSteps { get; set; }
	public string RoadmapNote { get; set; }
}
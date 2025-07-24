using System.ComponentModel.DataAnnotations.Schema;

namespace CohesionX.UserManagement.Domain.Entities;

public class UserVerificationRequirement : BaseEntity
{
	public bool RequireIdDocument { get; set; }
	public bool RequirePhotoUpload { get; set; }
	public bool RequirePhoneVerification { get; set; }
	public bool RequireEmailVerification { get; set; }

	public string VerificationLevel { get; set; } = default!;

	// Stored as JSON in DB
	public string ValidationRulesJson { get; set; } = "{}";

	public string Reason { get; set; } = default!;

	[NotMapped]
	public Dictionary<string, string> ValidationRules
	{
		get => string.IsNullOrWhiteSpace(ValidationRulesJson)
			? new()
			: System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(ValidationRulesJson)!;
		set => ValidationRulesJson = System.Text.Json.JsonSerializer.Serialize(value);
	}
}
using System.ComponentModel.DataAnnotations.Schema;

namespace CohesionX.UserManagement.Database.Abstractions.Entities;

/// <summary>
/// Defines the requirements a user must fulfill to be verified,
/// including flags for required documents and serialized validation rules.
/// </summary>
public class UserVerificationRequirement : BaseEntity
{
	/// <summary>
	/// Gets or sets a value indicating whether a government-issued ID document is required.
	/// </summary>
	public bool RequireIdDocument { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether a user photo upload is required.
	/// </summary>
	public bool RequirePhotoUpload { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether phone number verification is required.
	/// </summary>
	public bool RequirePhoneVerification { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether email verification is required.
	/// </summary>
	public bool RequireEmailVerification { get; set; }

	/// <summary>
	/// Gets or sets the level of verification this configuration applies to
	/// (e.g., "Basic", "Professional", "Admin").
	/// </summary>
	public string VerificationLevel { get; set; } = default!;

	/// <summary>
	/// Gets or sets the serialized JSON string that contains additional validation rules.
	/// Stored in the database as a raw JSON string.
	/// </summary>
	public string ValidationRulesJson { get; set; } = "{}";

	/// <summary>
	/// Gets or sets a description or justification for why these verification requirements exist.
	/// </summary>
	public string Reason { get; set; } = default!;

	/// <summary>
	/// Gets or sets a dictionary of validation rules, deserialized from <see cref="ValidationRulesJson"/>.
	/// This property is not mapped to the database.
	/// </summary>
	[NotMapped]
	public Dictionary<string, string> ValidationRules
	{
		get => string.IsNullOrWhiteSpace(ValidationRulesJson)
			? new ()
			: System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(ValidationRulesJson) !;
		set => ValidationRulesJson = System.Text.Json.JsonSerializer.Serialize(value);
	}
}

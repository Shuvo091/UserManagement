namespace CohesionX.UserManagement.Abstractions.DTOs.Options;

/// <summary>
/// Represents global validation options that determine which verification steps are required for a user.
/// These options can be configured based on the verification policy level or context.
/// </summary>
public class ValidationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether indicates whether the user must upload a valid ID document.
    /// </summary>
    public bool RequireIdDocument { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether the user must upload a profile photo.
    /// </summary>
    public bool RequirePhotoUpload { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether the user must verify their phone number.
    /// </summary>
    public bool RequirePhoneVerification { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether the user must verify their email address.
    /// </summary>
    public bool RequireEmailVerification { get; set; }

    /// <summary>
    /// Gets or sets specifies the level of verification required (e.g., Basic, Advanced).
    /// </summary>
    public string VerificationLevel { get; set; } = default!;

    /// <summary>
    /// Gets or sets validation rules.
    /// </summary>
    public ValidationRules ValidationRules { get; set; } = new ValidationRules();

    /// <summary>
    /// Gets or sets provides a reason or description explaining why this validation is required.
    /// </summary>
    public string Reason { get; set; } = default!;
}
namespace CohesionX.UserManagement.Abstractions.DTOs.Options;

/// <summary>
/// Represents specific validation rule values, such as required data or content for each check.
/// </summary>
public class ValidationRules
{
    /// <summary>
    /// Gets or sets the identification number to validate (e.g., national ID or passport number).
    /// </summary>
    public string IdNumber { get; set; } = default!;

    /// <summary>
    /// Gets or sets base64-encoded or URI of the uploaded photo used for verification.
    /// </summary>
    public string Photo { get; set; } = default!;

    /// <summary>
    /// Gets or sets additional notes or context provided by the user or system during validation.
    /// </summary>
    public string Note { get; set; } = default!;
}
namespace CohesionX.UserManagement.Domain.Entities;

/// <summary>
/// Represents a record of a user's verification attempt or status,
/// including metadata such as type, level, status, and verification data.
/// </summary>
public class VerificationRecord : BaseEntity
{
	/// <summary>
	/// Gets or sets the unique identifier of the user associated with this verification record.
	/// </summary>
	public Guid UserId { get; set; }

	/// <summary>
	/// Gets or sets the type of verification performed 
	/// (e.g., "ID", "Photo", "Phone", "Email", "Professional").
	/// </summary>
	public string VerificationType { get; set; } = default!;

	/// <summary>
	/// Gets or sets the current status of the verification 
	/// (e.g., "Pending", "Approved", "Rejected").
	/// </summary>
	public string Status { get; set; } = default!;

	/// <summary>
	/// Gets or sets the verification level this record applies to 
	/// (e.g., "Basic", "Professional").
	/// </summary>
	public string VerificationLevel { get; set; } = default!;

	/// <summary>
	/// Gets or sets the serialized verification data (e.g., uploaded document references, metadata).
	/// The format is JSON and structure depends on the verification type.
	/// </summary>
	public string VerificationData { get; set; } = default!;

	/// <summary>
	/// Gets or sets the timestamp (UTC) when the verification was successfully completed.
	/// Null if not yet verified.
	/// </summary>
	public DateTime? VerifiedAt { get; set; }

	/// <summary>
	/// Gets or sets the timestamp (UTC) when the verification record was created.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Navigation property to the user associated with this verification.
	/// </summary>
	public User User { get; set; } = default!;
}

namespace CohesionX.UserManagement.Database.Abstractions.Entities;

/// <summary>
/// Represents an audit log entry for user actions and system events.
/// </summary>
public class AuditLog : BaseEntity
{
	/// <summary>
	/// Gets or sets the unique identifier of the user associated with the log entry.
	/// </summary>
	public Guid UserId { get; set; }

	/// <summary>
	/// Gets or sets the action performed by the user.
	/// </summary>
	public string Action { get; set; } = default!;

	/// <summary>
	/// Gets or sets the details of the action, serialized as JSON.
	/// </summary>
	public string DetailsJson { get; set; } = default!;

	/// <summary>
	/// Gets or sets the IP address from which the action was performed.
	/// </summary>
	public string IpAddress { get; set; } = default!;

	/// <summary>
	/// Gets or sets the user agent string of the client performing the action.
	/// </summary>
	public string UserAgent { get; set; } = default!;

	/// <summary>
	/// Gets or sets the timestamp of when the action occurred.
	/// </summary>
	public DateTime Timestamp { get; set; }

	/// <summary>
	/// Gets or sets the navigation property for the associated user.
	/// </summary>
	public User User { get; set; } = default!;
}

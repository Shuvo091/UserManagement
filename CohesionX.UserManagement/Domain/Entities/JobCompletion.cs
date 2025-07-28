namespace CohesionX.UserManagement.Domain.Entities;

/// <summary>
/// Represents the completion of a transcription or evaluation job by a user,
/// including Elo change, outcome, and associated comparison metadata.
/// </summary>
public class JobCompletion : BaseEntity
{
	/// <summary>
	/// Gets or sets the unique identifier of the user who completed the job.
	/// </summary>
	public Guid UserId { get; set; }

	/// <summary>
	/// Gets or sets the identifier of the completed job.
	/// </summary>
	public string JobId { get; set; } = default!;

	/// <summary>
	/// Gets or sets the outcome of the job (e.g., "win", "loss", "draw", "pass").
	/// </summary>
	public string Outcome { get; set; } = default!;

	/// <summary>
	/// Gets or sets the Elo rating change resulting from the job.
	/// This value may be positive (gain), negative (loss), or zero.
	/// </summary>
	public int EloChange { get; set; }

	/// <summary>
	/// Gets or sets the unique identifier of the comparison session
	/// related to the job completion.
	/// </summary>
	public Guid ComparisonId { get; set; } = default!;

	/// <summary>
	/// Gets or sets the timestamp (UTC) when the job was marked as completed.
	/// </summary>
	public DateTime CompletedAt { get; set; }

	/// <summary>
	/// Gets or sets the timestamp (UTC) when the job completion record was created.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Navigation property to the user who completed the job.
	/// </summary>
	public User User { get; set; } = default!;

	/// <summary>
	/// Navigation property to the peer or reviewer involved in the comparison.
	/// </summary>
	public User Comparison { get; set; } = default!;
}

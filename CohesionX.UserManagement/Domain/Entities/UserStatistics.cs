namespace CohesionX.UserManagement.Domain.Entities;

/// <summary>
/// Represents statistical performance data for a user, including Elo rating and job activity metrics.
/// </summary>
public class UserStatistics : BaseEntity
{
	/// <summary>
	/// Gets or sets the unique identifier of the user these statistics belong to.
	/// </summary>
	public Guid UserId { get; set; }

	/// <summary>
	/// Gets or sets the total number of jobs completed by the user.
	/// </summary>
	public int TotalJobs { get; set; }

	/// <summary>
	/// Gets or sets the user's current Elo rating.
	/// </summary>
	public int CurrentElo { get; set; }

	/// <summary>
	/// Gets or sets the highest Elo rating the user has achieved to date.
	/// </summary>
	public int PeakElo { get; set; }

	/// <summary>
	/// Gets or sets the total number of Elo-based comparison games played by the user.
	/// </summary>
	public int GamesPlayed { get; set; }

	/// <summary>
	/// Gets or sets the timestamp (UTC) of the last time Elo was recalculated.
	/// </summary>
	public DateTime LastCalculated { get; set; }

	/// <summary>
	/// Gets or sets the timestamp (UTC) when this statistics record was created.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Gets or sets the timestamp (UTC) when this statistics record was last updated.
	/// </summary>
	public DateTime UpdatedAt { get; set; }

	/// <summary>
	/// Gets or sets navigation property to the user associated with these statistics.
	/// </summary>
	public User User { get; set; } = default!;
}

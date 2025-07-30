namespace CohesionX.UserManagement.Application.Models;

/// <summary>
/// Represents options for application constants.
/// </summary>
public class AppConstantsOptions
{
	/// <summary>
	/// Gets or sets a value indicating whether gets or sets the default language for the application.
	/// </summary>
	public bool EnableGrpc { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether gets or sets the default language for the application.
	/// </summary>
	public bool EnableIdDocumentCollection { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether gets or sets the default language for the application.
	/// </summary>
	public string PopiaComplianceMode { get; set; } = default!;

	/// <summary>
	/// Gets or sets the initial Elo rating for new users.
	/// </summary>
	public int InitialEloRating { get; set; }

	/// <summary>
	/// Gets or sets the minimum Elo rating required for a user to be considered a pro player.
	/// </summary>
	public int MinEloRequiredForPro { get; set; }

	/// <summary>
	/// Gets or sets the minimum number of jobs required for a user to be considered a pro player.
	/// </summary>
	public int MinJobsRequiredForPro { get; set; }

	/// <summary>
	/// Gets or sets the K-factor for Elo rating calculations based on user experience level.
	/// </summary>
	public int EloKFactorNew { get; set; }

	/// <summary>
	/// Gets or sets the K-factor for Elo rating calculations for established players.
	/// </summary>
	public int EloKFactorEstablished { get; set; }

	/// <summary>
	/// Gets or sets the K-factor for Elo rating calculations for expert players.
	/// </summary>
	public int EloKFactorExpert { get; set; }

	/// <summary>
	/// Gets or sets the timeout in hours for job processing.
	/// </summary>
	public int JobTimeoutHours { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of concurrent jobs that can be processed at the same time.
	/// </summary>
	public int MaxConcurrentJobs { get; set; }

	/// <summary>
	/// Gets or sets the time-to-live (TTL) in minutes for Redis cache entries.
	/// </summary>
	public int RedisCacheTtlMinutes { get; set; }

	/// <summary>
	/// Gets or sets the default number of minutes for booking out a job.
	/// </summary>
	public int DefaultBookoutMinutes { get; set; }
}

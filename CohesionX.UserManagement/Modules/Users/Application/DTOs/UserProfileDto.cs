namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class GetUserProfileResponse
{
	public Guid UserId { get; set; }
	public UserProfileDto Profile { get; set; } = new();
}

public class UserProfileDto
{
	public string FirstName { get; set; } = default!;
	public string LastName { get; set; } = default!;
	public int EloRating { get; set; }
	public int PeakElo { get; set; }
	public string Status { get; set; } = default!;
	public DateTime RegisteredAt { get; set; }
	public bool IsProfessional { get; set; }
	public ProfessionalEligibilityDto ProfessionalEligibility { get; set; } = new();
	public UserStatisticsDto Statistics { get; set; } = new();
	public UserPreferencesDto Preferences { get; set; } = new();
}

public class ProfessionalEligibilityDto
{
	public bool Eligible { get; set; }
	public List<string> MissingCriteria { get; set; } = new();
	public ProfessionalProgressDto Progress { get; set; } = new();
}

public class ProfessionalProgressDto
{
	public string EloProgress { get; set; } = default!;
	public string JobsProgress { get; set; } = default!;
}

public class UserStatisticsDto
{
	public int TotalJobsCompleted { get; set; }
	public int GamesPlayed { get; set; }
	public string EloTrend { get; set; } = default!;
	public List<string> DialectExpertise { get; set; } = new();
	public double WinRate { get; set; }
	public Last30DaysStatsDto Last30Days { get; set; } = new();
}

public class Last30DaysStatsDto
{
	public int JobsCompleted { get; set; }
	public string EloChange { get; set; } = default!;
	public double Earnings { get; set; }
}

public class UserPreferencesDto
{
	public int MaxConcurrentJobs { get; set; }
	public List<string> DialectPreferences { get; set; } = new();
	public List<string> PreferredJobTypes { get; set; } = new();
}

namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class UserAvailabilityResponse
{
	public List<AvailableUsersDto> AvailableUsers { get; set; } = [];
	public int TotalAvailable { get; set; }
	public DateTime QueryTimestamp { get; set; }
}

public class AvailableUsersDto
{
	public Guid UserId { get; set; }
	public int EloRating { get; set; }
	public int PeakElo { get; set; }
	public List<string> DialectExpertise { get; set; } = [];
	public int CurrentWorkload { get; set; }
	public string RecentPerformance { get; set; } = "";
	public int GamesPlayed { get; set; }
	public string Role { get; set; } = "";
	public bool BypassQaComparison { get; set; }
	public DateTime LastActive { get; set; }
}

public class UserAvailabilityRedisDto
{
	public string Status { get; set; } = default!;
	public int MaxConcurrentJobs { get; set; } = 3;
	public int CurrentWorkload { get; set; } = 0;
	public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
}
namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class AvailableUserResponseDto
{
	public Guid UserId { get; set; }
	public int EloRating { get; set; }
	public int PeakElo { get; set; }
	public List<string> DialectExpertise { get; set; } = new();
	public int CurrentWorkload { get; set; }
	public string RecentPerformance { get; set; } = string.Empty;
	public int GamesPlayed { get; set; }
	public string Role { get; set; } = default!;
	public bool BypassQaComparison { get; set; }
	public DateTime LastActive { get; set; }
}

public class UserAvailabilityDto
{
	public string Status { get; set; } = default!;
	public int MaxConcurrentJobs { get; set; } = 3;
	public int CurrentWorkload { get; set; } = 0;
	public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
}

public class UserWithEloAndDialectsDto
{
	public Guid UserId { get; set; }
	public int EloRating { get; set; }
	public int PeakElo { get; set; }
	public int GamesPlayed { get; set; }
	public List<string> DialectCodes { get; set; } = new();
	public bool IsProfessional { get; set; }
	public string UserRole { get; set; } = string.Empty;
}
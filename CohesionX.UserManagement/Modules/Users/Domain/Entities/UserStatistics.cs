namespace CohesionX.UserManagement.Modules.Users.Domain.Entities;

public class UserStatistics
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public int TotalJobs { get; set; }
	public int CurrentElo { get; set; }
	public int PeakElo { get; set; }
	public int GamesPlayed { get; set; }
	public DateTime LastCalculated { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }

	public User User { get; set; } = default!;
}

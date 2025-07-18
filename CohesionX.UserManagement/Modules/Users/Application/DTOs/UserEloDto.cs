namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class UserEloDto
{
	public int CurrentElo { get; set; }
	public int PeakElo { get; set; }
	public int GamesPlayed { get; set; }
	public string RecentTrend { get; set; } = "";
	public DateTime LastJobCompleted { get; set; }
}
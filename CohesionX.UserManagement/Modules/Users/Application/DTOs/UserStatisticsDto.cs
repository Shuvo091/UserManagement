namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;
public class UserStatisticsDto
{
	public int TotalJobs { get; set; }
	public int CurrentElo { get; set; }
	public int PeakElo { get; set; }
	public int GamesPlayed { get; set; }
	public DateTime LastCalculated { get; set; }
}
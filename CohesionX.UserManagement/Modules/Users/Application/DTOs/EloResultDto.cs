namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class EloResultDto
{
	public Guid TranscriberId { get; set; }
	public int OldElo { get; set; }
	public int NewElo { get; set; }
	public int EloChange { get; set; }
	public string Outcome { get; set; } = default!;
}
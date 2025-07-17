namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class EloChangeDto
{
	public Guid TranscriberId { get; set; }
	public int OldElo { get; set; }
	public int OpponentElo { get; set; }
	public string Outcome { get; set; } = default!; // "win", "loss"
}
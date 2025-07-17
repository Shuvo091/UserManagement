namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class EloHistoryDto
{
	public int OldElo { get; set; }
	public int NewElo { get; set; }
	public string Reason { get; set; } = default!;
	public string ComparisonId { get; set; } = default!;
	public string JobId { get; set; } = default!;
	public string Outcome { get; set; } = default!;
	public string ComparisonType { get; set; } = default!;
	public int KFactorUsed { get; set; }
	public DateTime ChangedAt { get; set; }
}
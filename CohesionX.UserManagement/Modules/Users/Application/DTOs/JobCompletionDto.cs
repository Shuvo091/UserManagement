namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;
public class JobCompletionDto
{
	public string JobId { get; set; } = default!;
	public string Outcome { get; set; } = default!;
	public int EloChange { get; set; }
	public string CompletionId { get; set; } = default!;
	public DateTime CompletedAt { get; set; }
}
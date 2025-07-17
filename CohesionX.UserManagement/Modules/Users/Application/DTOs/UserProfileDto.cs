namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class UserProfileDto
{
	public Guid UserId { get; set; }
	public string FirstName { get; set; } = default!;
	public string LastName { get; set; } = default!;
	public string Email { get; set; } = default!;
	public string Phone { get; set; } = default!;
	public string IdNumber { get; set; } = default!;
	public string Status { get; set; } = default!;
	public bool IsProfessional { get; set; }
	public int EloRating { get; set; }
	public int PeakElo { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public int GamesPlayed { get; set; }

	public List<UserDialectDto> Dialects { get; set; } = [];
	public UserStatisticsDto? Statistics { get; set; }
	public List<EloHistoryDto> EloHistories { get; set; } = [];
	public List<JobCompletionDto> JobCompletions { get; set; } = [];
	public List<JobClaimDto> JobClaims { get; set; } = [];
	public List<AuditLogDto> AuditLogs { get; set; } = [];
	public List<VerificationRecordDto> VerificationRecords { get; set; } = [];
	public bool IsVerified { get; set; }
}
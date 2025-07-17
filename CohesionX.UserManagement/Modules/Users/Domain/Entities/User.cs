namespace CohesionX.UserManagement.Modules.Users.Domain.Entities;
public class User
{
	public Guid Id { get; set; }
	public string FirstName { get; set; } = default!;
	public string LastName { get; set; } = default!;
	public string Email { get; set; } = default!;
	public string Phone { get; set; } = default!;
	public string IdNumber { get; set; } = default!;
	public int EloRating { get; set; }
	public int PeakElo { get; set; }
	public string Status { get; set; } = default!;
	public bool IsProfessional { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public int GamesPlayed { get; set; }
	public ICollection<UserDialect> Dialects { get; set; } = [];
	public UserStatistics? Statistics { get; set; }
	public ICollection<EloHistory> EloHistories { get; set; } = [];
	public ICollection<JobCompletion> JobCompletions { get; set; } = [];
	public ICollection<JobClaim> JobClaims { get; set; } = [];
	public ICollection<AuditLog> AuditLogs { get; set; } = [];
	public ICollection<VerificationRecord> VerificationRecords { get; set; } = [];
}

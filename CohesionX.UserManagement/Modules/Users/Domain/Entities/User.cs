using CohesionX.UserManagement.Modules.Users.Domain.Constants;

namespace CohesionX.UserManagement.Modules.Users.Domain.Entities;

public class User
{
	// Core Properties
	public Guid Id { get; set; }
	public string FirstName { get; set; } = default!;
	public string LastName { get; set; } = default!;
	public string Email { get; set; } = default!;
	public string Phone { get; set; } = default!;
	public string IdNumber { get; set; } = default!;
	public string Status { get; set; } = UserStatus.PENDING_VERIFICATION;
	public string Role { get; set; } = UserRole.TRANSCRIBER;
	public bool IsProfessional { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	// Navigation Properties
	public ICollection<UserDialect> Dialects { get; set; } = [];
	public UserStatistics? Statistics { get; set; }
	public ICollection<EloHistory> EloHistories { get; set; } = [];
	public ICollection<JobCompletion> JobCompletions { get; set; } = [];
	public ICollection<JobClaim> JobClaims { get; set; } = [];
	public ICollection<AuditLog> AuditLogs { get; set; } = [];
	public ICollection<VerificationRecord> VerificationRecords { get; set; } = [];


	//public DateTime? DateOfBirth { get; set; }
	//public bool IsActive { get; set; } = false;
	//public DateTime? ActivatedAt { get; set; }
	//public string? ActivationMethod { get; set; }
	//public string? VerificationLevel { get; set; } = "basic_v1";

	//// Game Properties
	//public int GamesPlayed { get; set; }

	//// PII and Compliance
	//public bool ConsentToPIICollection { get; set; }
	//public string? Address { get; set; }
	//public string? IdPhotoPath { get; set; }

}
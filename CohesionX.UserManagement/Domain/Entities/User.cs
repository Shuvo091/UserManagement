using SharedLibrary.AppEnums;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Domain.Entities;

public class User : BaseEntity
{
	// Core Properties
	public string FirstName { get; set; } = default!;
	public string LastName { get; set; } = default!;
	public string Email { get; set; } = default!;
	public string UserName { get; set; } = default!;
	public string PasswordHash { get; set; } = default!;
	public string? Phone { get; set; } = default!;
	public string? IdNumber { get; set; } = default!;
	public string Status { get; set; } = UserStatusType.PendingVerification.ToDisplayName();
	public string Role { get; set; } = UserRoleType.Transcriber.ToDisplayName();
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
}
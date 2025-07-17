using CohesionX.UserManagement.Shared.Constants;

namespace CohesionX.UserManagement.Modules.Users.Domain.Entities;

public class User
{
	// Core Properties
	public Guid Id { get; set; }
	public string FirstName { get; set; } = default!;
	public string LastName { get; set; } = default!;
	public string Email { get; set; } = default!;
	public string Phone { get; set; } = default!;
	public string PasswordHash { get; set; } = default!; // Added for security
	public string SouthAfricanIdNumber { get; set; } = default!;
	public DateTime DateOfBirth { get; set; }

	// Role Property
	public UserRole UserRole { get; set; } = UserRole.Transcriber;

	// Status Properties
	public string Status { get; set; } = "pending_verification"; // Default status
	public bool IsActive { get; set; } = false;
	public bool IsProfessional { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	public DateTime? ActivatedAt { get; set; } // Made nullable
	public string? ActivationMethod { get; set; }
	public string? VerificationLevel { get; set; } = "basic_v1";

	// Game Properties
	public int EloRating { get; set; } = 1200; // Default ELO
	public int PeakElo { get; set; } = 1200;
	public int GamesPlayed { get; set; }

	// PII and Compliance
	public bool ConsentToPIICollection { get; set; }
	public string? Address { get; set; }
	public string? IdPhotoPath { get; set; }

	// Navigation Properties
	public ICollection<UserDialect> Dialects { get; set; } = [];
	public UserStatistics? Statistics { get; set; }
	public ICollection<EloHistory> EloHistories { get; set; } = [];
	public ICollection<JobCompletion> JobCompletions { get; set; } = [];
	public ICollection<JobClaim> JobClaims { get; set; } = [];
	public ICollection<AuditLog> AuditLogs { get; set; } = [];
	public ICollection<VerificationRecord> VerificationRecords { get; set; } = [];

	// Methods
	public bool TryActivateUser(out string? reason)
	{
		reason = null;

		if (IsActive)
		{
			reason = "User is already active";
			return false;
		}

		if (string.IsNullOrEmpty(IdPhotoPath))
		{
			reason = "ID photo is required";
			return false;
		}

		if (!ValidateIdFormat())
		{
			reason = "Invalid ID format";
			return false;
		}

		if (!IsAtLeast18())
		{
			reason = "User must be at least 18 years old";
			return false;
		}

		// All checks passed - activate user
		Status = "active";
		IsActive = true;
		ActivatedAt = DateTime.UtcNow;
		ActivationMethod = "automatic";
		VerificationLevel = "basic_v1";
		UpdatedAt = DateTime.UtcNow;

		return true;
	}

	public bool ValidateIdFormat()
	{
		if (string.IsNullOrEmpty(SouthAfricanIdNumber))
			return false;

		// Basic SA ID format validation:
		// 1. Must be 13 digits
		// 2. First 6 digits must be a valid date (YYMMDD)
		// 3. 7th digit indicates citizenship (0-2)
		// 4. Last digit is checksum (Luhn algorithm would be better for V2)
		return SouthAfricanIdNumber.Length == 13 &&
			   SouthAfricanIdNumber.All(char.IsDigit) &&
			   IsValidBirthDateInId();
	}

	private bool IsValidBirthDateInId()
	{
		if (SouthAfricanIdNumber.Length < 6)
			return false;

		var yearPart = int.Parse(SouthAfricanIdNumber.Substring(0, 2));
		var monthPart = int.Parse(SouthAfricanIdNumber.Substring(2, 2));
		var dayPart = int.Parse(SouthAfricanIdNumber.Substring(4, 2));

		// Convert 2-digit year to 4-digit (00-99 becomes 2000-2099)
		var fullYear = 2000 + yearPart;

		try
		{
			var birthDate = new DateTime(fullYear, monthPart, dayPart);
			return birthDate <= DateTime.Today;
		}
		catch
		{
			return false;
		}
	}

	public bool IsAtLeast18()
	{
		var today = DateTime.Today;
		var age = today.Year - DateOfBirth.Year;

		// Subtract a year if the birthday hasn't occurred yet this year
		if (DateOfBirth.Date > today.AddYears(-age))
			age--;

		return age >= 18;
	}

	public bool HasCompletedBasicVerification()
	{
		return !string.IsNullOrEmpty(IdPhotoPath) &&
			   ValidateIdFormat() &&
			   IsAtLeast18();
	}
}
using System.ComponentModel.DataAnnotations;

namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class UserRegisterRequest
{
	[Required]
	public string IdNumber { get; set; } = default!;
	[Required]
	public string FirstName { get; set; } = default!;
	[Required]
	public string LastName { get; set; } = default!;
	[Required, EmailAddress]
	public string Email { get; set; } = default!;
	[Required]
	public string Phone { get; set; } = default!; // Added
	[Required]
	public List<string> DialectPreferences { get; set; } = new(); // Added
	[Required]
	public string LanguageExperience { get; set; } = default!; // Added
	public bool consentToDataProcessing { get; set; }
}

public class UserRegisterResponse
{
	public Guid UserId { get; set; }
	public int EloRating { get; set; }
	public string Status { get; set; } = default!;
	public string ProfileUri { get; set; } = default!;
	public List<string> VerificationRequired { get; set; } = [];
}
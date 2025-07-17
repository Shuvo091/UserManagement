using System.ComponentModel.DataAnnotations;

namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class UserRegisterDto
{
	[Required]
	public string IdNumber { get; set; } = default!;
	[Required]
	public string FirstName { get; set; } = default!;
	[Required]
	public string LastName { get; set; } = default!;
	[Required]
	public string Email { get; set; } = default!;
	[Required]
	public string Password { get; set; } = default!;
	[Required]
	public string Phone { get; set; } = default!; // Added
	[Required]
	public List<string>? DialectPreferences { get; set; } = new(); // Added
	[Required]
	public string LanguageExperience { get; set; } = default!; // Added
	public DateTime? DateOfBirth { get; set; }
	public IFormFile? IdPhoto { get; set; }
	public bool ConsentToPIICollection { get; set; }
	public string? Address { get; set; }
}
namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class UserRegisterDto
{
	public string IdNumber { get; set; } = default!;
	public string FirstName { get; set; } = default!;
	public string LastName { get; set; } = default!;
	public string Email { get; set; } = default!;
	public string PhoneNumber { get; set; } = default!;
}

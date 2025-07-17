namespace CohesionX.UserManagement.Modules.Users.Domain.Entities;

public class User
{
	public Guid Id { get; set; }
	public string IdNumber { get; set; } = default!;
	public string FirstName { get; set; } = default!;
	public string LastName { get; set; } = default!;
	public string Email { get; set; } = default!;
	public string PhoneNumber { get; set; } = default!;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

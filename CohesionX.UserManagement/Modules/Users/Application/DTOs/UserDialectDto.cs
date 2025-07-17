namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class UserDialectDto
{
	public string Dialect { get; set; } = default!;
	public string ProficiencyLevel { get; set; } = default!;
	public bool IsPrimary { get; set; }
}
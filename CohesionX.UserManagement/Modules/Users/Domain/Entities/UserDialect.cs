namespace CohesionX.UserManagement.Modules.Users.Domain.Entities;
public class UserDialect
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public string DialectCode { get; set; } = default!;
	public string ProficiencyLevel { get; set; } = default!;
	public bool IsPrimary { get; set; }
	public DateTime CreatedAt { get; set; }

	public User User { get; set; } = default!;
}

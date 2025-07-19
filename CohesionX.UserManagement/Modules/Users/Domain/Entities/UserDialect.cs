namespace CohesionX.UserManagement.Modules.Users.Domain.Entities;
public class UserDialect : BaseEntity
{
	public Guid UserId { get; set; }
	public string Dialect { get; set; } = default!;
	public string ProficiencyLevel { get; set; } = default!;
	public bool IsPrimary { get; set; }
	public DateTime CreatedAt { get; set; }

	public User User { get; set; } = default!;
}

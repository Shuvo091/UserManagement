namespace CohesionX.UserManagement.Domain.Entities;

/// <summary>
/// Provides a base entity with a unique identifier for all domain entities.
/// </summary>
public abstract class BaseEntity
{
	/// <summary>
	/// Gets or sets the unique identifier for the entity.
	/// </summary>
	public Guid Id { get; set; }
}

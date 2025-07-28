using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.UserManagement.Persistence.Interfaces
{
	/// <summary>
	/// Repository interface for managing <see cref="UserVerificationRequirement"/> entities,
	/// extending the generic <see cref="IRepository{T}"/> interface.
	/// </summary>
	public interface IVerificationRequirementRepository : IRepository<UserVerificationRequirement>
	{
		/// <summary>
		/// Retrieves the current global user verification requirement configuration.
		/// </summary>
		/// <returns>
		/// A task representing the asynchronous operation, containing the <see cref="UserVerificationRequirement"/> if found; otherwise, <c>null</c>.
		/// </returns>
		Task<UserVerificationRequirement?> GetVerificationRequirement();
	}
}

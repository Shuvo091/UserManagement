using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.UserManagement.Application.Interfaces;

/// <summary>
/// Provides operations for retrieving user verification requirements.
/// </summary>
public interface IVerificationRequirementService
{
	/// <summary>
	/// Gets the current verification requirements for users.
	/// </summary>
	/// <returns>
	/// The <see cref="UserVerificationRequirement"/> entity containing verification rules,
	/// or <c>null</c> if not configured.
	/// </returns>
	Task<UserVerificationRequirement?> GetVerificationRequirement();
}

using CohesionX.UserManagement.Database.Abstractions.Entities;

namespace CohesionX.UserManagement.Abstractions.Services;

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

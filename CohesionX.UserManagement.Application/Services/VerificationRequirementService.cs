
using CohesionX.UserManagement.Abstractions.Services;
using CohesionX.UserManagement.Database.Abstractions.Entities;

namespace CohesionX.UserManagement.Application.Services
{
	/// <summary>
	/// Provides operations for retrieving user verification requirements.
	/// </summary>
	public class VerificationRequirementService : IVerificationRequirementService
	{
		private readonly IVerificationRequirementRepository _repo;

		/// <summary>
		/// Initializes a new instance of the <see cref="VerificationRequirementService"/> class.
		/// </summary>
		/// <param name="repo">The repository for verification requirements.</param>
		public VerificationRequirementService(IVerificationRequirementRepository repo)
		{
			_repo = repo;
		}

		/// <summary>
		/// Gets the current verification requirements for users.
		/// </summary>
		/// <returns>
		/// The <see cref="UserVerificationRequirement"/> entity containing verification rules,
		/// or <c>null</c> if not configured.
		/// </returns>
		public async Task<UserVerificationRequirement?> GetVerificationRequirement()
		{
			return await _repo.GetVerificationRequirement();
		}
	}
}

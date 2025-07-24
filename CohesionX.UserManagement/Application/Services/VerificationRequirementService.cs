using CohesionX.UserManagement.Application.Interfaces;
using CohesionX.UserManagement.Domain.Entities;
using CohesionX.UserManagement.Persistence.Interfaces;

namespace CohesionX.UserManagement.Application.Services
{
	public class VerificationRequirementService : IVerificationRequirementService
	{
		private readonly IVerificationRequirementRepository _repo;

		public VerificationRequirementService(IVerificationRequirementRepository repo)
		{
			_repo = repo;
		}
		public async Task<UserVerificationRequirement?> GetVerificationRequirement()
		{
			return await _repo.GetVerificationRequirement();
		}
	}
}

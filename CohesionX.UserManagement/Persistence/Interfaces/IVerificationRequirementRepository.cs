using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.UserManagement.Persistence.Interfaces
{
	public interface IVerificationRequirementRepository : IRepository<UserVerificationRequirement>
	{
		Task<UserVerificationRequirement?> GetVerificationRequirement();
	}
}

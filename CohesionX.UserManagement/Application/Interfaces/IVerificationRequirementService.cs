using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.UserManagement.Application.Interfaces;

public interface IVerificationRequirementService
{
	Task<UserVerificationRequirement?> GetVerificationRequirement();
}

using CohesionX.UserManagement.Domain.Entities;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Application.Interfaces
{
	public interface IUserService
	{
		Task<UserRegisterResponse> RegisterUserAsync(UserRegisterRequest dto);
		Task<VerificationResponse> ActivateUser(User user, VerificationRequest verificationDto);
		Task<bool> CheckIdNumber(Guid userId, string idNumber);
		Task<UserProfileResponse> GetProfileAsync(Guid userId);
		Task<GetProfessionalStatusResponse> GetProfessionalStatus(Guid userId);
		Task<User> GetUserAsync(Guid userId);
		Task<User> GetUserByEmailAsync(string email);
		Task<List<User>> GetFilteredUser(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit);
		Task UpdateAvailabilityAuditAsync(Guid userId, UserAvailabilityRedisDto existingAvailability, string? ipAddress, string? userAgent);
		Task ClaimJobAsync(Guid userId, Guid claimId, ClaimJobRequest claimJobRequest, DateTime bookouExpiresAt);
		Task<ValidateTiebreakerClaimResponse> ValidateTieBreakerClaim(Guid userId, ValidateTiebreakerClaimRequest validationReq);
		Task<SetProfessionalResponse> SetProfessional(Guid userId, SetProfessionalRequest validationReq);
	}
}

using CohesionX.UserManagement.Database.Abstractions.Entities;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Abstractions.Services;

/// <summary>
/// Provides operations for user registration, profile management, verification, professional status, and job claims.
/// </summary>
public interface IUserService
{
	/// <summary>
	/// Registers a new user.
	/// </summary>
	/// <param name="dto">The user registration request data.</param>
	/// <returns>The response containing user registration details.</returns>
	Task<UserRegisterResponse> RegisterUserAsync(UserRegisterRequest dto);

	/// <summary>
	/// Activates a user after verification.
	/// </summary>
	/// <param name="user">The user entity to activate.</param>
	/// <param name="verificationDto">The verification request details.</param>
	/// <returns>The verification response.</returns>
	Task<VerificationResponse> ActivateUser(User user, VerificationRequest verificationDto);

	/// <summary>
	/// Checks if the provided ID number matches the user's record.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <param name="idNumber">The ID number to check.</param>
	/// <returns><c>true</c> if the ID number matches; otherwise, <c>false</c>.</returns>
	Task<bool> CheckIdNumber(Guid userId, string idNumber);

	/// <summary>
	/// Gets the profile information for a user.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <returns>The user's profile response.</returns>
	Task<UserProfileResponse> GetProfileAsync(Guid userId);

	/// <summary>
	/// Gets the professional status for a user.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <returns>The professional status response.</returns>
	Task<GetProfessionalStatusResponse> GetProfessionalStatus(Guid userId);

	/// <summary>
	/// Gets the user entity by user ID.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <returns>The user entity.</returns>
	Task<User> GetUserAsync(Guid userId);

	/// <summary>
	/// Gets the user entity by email address.
	/// </summary>
	/// <param name="email">The user's email address.</param>
	/// <returns>The user entity.</returns>
	Task<User> GetUserByEmailAsync(string email);

	/// <summary>
	/// Gets a filtered list of users based on dialect, Elo rating, workload, and limit.
	/// </summary>
	/// <param name="dialect">Dialect filter.</param>
	/// <param name="minElo">Minimum Elo rating.</param>
	/// <param name="maxElo">Maximum Elo rating.</param>
	/// <param name="maxWorkload">Maximum workload.</param>
	/// <param name="limit">Maximum number of users to return.</param>
	/// <returns>A list of filtered user entities.</returns>
	Task<List<User>> GetFilteredUser(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit);

	/// <summary>
	/// Updates the audit log for a user's availability change.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <param name="existingAvailability">The current availability data.</param>
	/// <param name="ipAddress">The IP address of the request.</param>
	/// <param name="userAgent">The user agent string of the request.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	Task UpdateAvailabilityAuditAsync(Guid userId, UserAvailabilityRedisDto existingAvailability, string? ipAddress, string? userAgent);

	/// <summary>
	/// Claims a job for a user.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <param name="claimId">The claim identifier.</param>
	/// <param name="claimJobRequest">The job claim request details.</param>
	/// <param name="bookouExpiresAt">The expiration time for the job claim.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	Task ClaimJobAsync(Guid userId, Guid claimId, ClaimJobRequest claimJobRequest, DateTime bookouExpiresAt);

	/// <summary>
	/// Validates a tiebreaker claim for a user.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <param name="validationReq">The tiebreaker claim request details.</param>
	/// <returns>The tiebreaker claim validation response.</returns>
	Task<ValidateTiebreakerClaimResponse> ValidateTieBreakerClaim(Guid userId, ValidateTiebreakerClaimRequest validationReq);

	/// <summary>
	/// Sets the professional status for a user.
	/// </summary>
	/// <param name="userId">The user's unique identifier.</param>
	/// <param name="validationReq">The request containing professional status details.</param>
	/// <returns>The response containing updated professional status.</returns>
	Task<SetProfessionalResponse> SetProfessional(Guid userId, SetProfessionalRequest validationReq);
}

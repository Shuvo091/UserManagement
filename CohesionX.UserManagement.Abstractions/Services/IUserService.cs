// <copyright file="IUserService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CohesionX.UserManagement.Database.Abstractions.Entities;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Contracts.Usermanagement.RedisDtos;
using SharedLibrary.Contracts.Usermanagement.Requests;
using SharedLibrary.Contracts.Usermanagement.Responses;

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
    /// Gets available users.
    /// </summary>
    /// <param name="dialect"> optional dialect.</param>
    /// <param name="minElo">optional min elo.</param>
    /// <param name="maxElo">optional max elo.</param>
    /// <param name="maxWorkload">optional maxworkload.</param>
    /// <param name="limit">optional limit.</param>
    /// <returns>user availability resp.</returns>
    Task<UserAvailabilityResponse> GetUserAvailabilitySummaryAsync(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit);

    /// <summary>
    /// Gets the user availability for a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>The user availability response.</returns>
    Task<UserAvailabilityRedisDto?> GetAvailabilityAsync(Guid userId);

    /// <summary>
    /// Patches a user availability.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="availabilityUpdateRequest"> request object.</param>
    /// <returns>user availability response.</returns>
    Task<UserAvailabilityUpdateResponse> PatchAvailabilityAsync(Guid userId, UserAvailabilityUpdateRequest availabilityUpdateRequest);

    /// <summary>
    /// Activates a user after verification.
    /// </summary>
    /// <param name="userId">The user entity to activate.</param>
    /// <param name="verificationDto">The verification request details.</param>
    /// <returns>The verification response.</returns>
    Task<VerificationResponse> ActivateUser(Guid userId, VerificationRequest verificationDto);

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
    /// Gets the professional status for multiple users.
    /// </summary>
    /// <param name="userIds">The users' unique identifier.</param>
    /// <returns>The professional status response for all.</returns>
    Task<ProfessionalStatusBatchResponse> GetBatchProfessionalStatus(List<Guid> userIds);

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
    /// <param name="claimJobRequest">The job claim request details.</param>
    /// <param name="originalTranscribers"> Optional: original transcriber for the job. </param>
    /// <param name="requiredMinElo"> Optional: required minimum elo for the job. </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<ClaimJobResponse> ClaimJobAsync(Guid userId, ClaimJobRequest claimJobRequest, List<Guid>? originalTranscribers = null, int? requiredMinElo = null);

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

    /// <summary>
    /// Authenticates a user login request.
    /// </summary>
    /// <param name="request"> username and password for logging in. </param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    Task<UserLoginResponse?> AuthenticateAsync(UserLoginRequest request);

    /// <summary>
    /// Changes the password for a user.
    /// </summary>
    /// <param name="currentPassword"> User's current password. </param>
    /// <param name="newPassword"> User's intended new password. </param>
    /// <returns>he response containing the success/failure of the request.</returns>
    Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(string currentPassword, string newPassword);
}

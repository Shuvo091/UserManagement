using System.Security.Claims;
using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SharedLibrary.AppEnums;
using SharedLibrary.Common.Utilities;
using SharedLibrary.Contracts.Usermanagement.Requests;

namespace CohesionX.UserManagement.Controllers;

/// <summary>
/// API controller for user management operations such as registration, verification, availability, and job claiming.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService userService;
    private readonly IEloService eloService;
    private readonly int defaultBookoutInMinutes;
    private readonly ILogger<UsersController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="userService">Service that handles user-related operations such as registration, updates, and retrieval.</param>
    /// <param name="eloService">Service that manages Elo rating calculations and history.</param>
    /// <param name="appContantOptions">Application configuration used to access settings.</param>
    /// <param name="logger"> logger. </param>
    public UsersController(
        IUserService userService,
        IEloService eloService,
        IOptions<AppConstantsOptions> appContantOptions,
        ILogger<UsersController> logger)
    {
        this.userService = userService;
        this.eloService = eloService;
        this.defaultBookoutInMinutes = appContantOptions.Value.DefaultBookoutMinutes;
        this.logger = logger;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="dto">The user registration request data.</param>
    /// <returns>The created user profile or error details.</returns>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterRequest dto)
    {
        if (!this.ModelState.IsValid)
        {
            this.logger.LogWarning($"Rejecting registration: Request object not valid. ModelState: {this.ModelState}");
            return this.BadRequest(this.ModelState);
        }

        var result = await this.userService.RegisterUserAsync(dto);
        return this.CreatedAtAction(nameof(this.GetProfile), new { userId = result.UserId }, result);
    }

    /// <summary>
    /// Log in request.
    /// </summary>
    /// <param name="request"> Username and password. </param>
    /// <returns>Upon successful request, gets a jwt token. </returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
    {
        if (!this.ModelState.IsValid)
        {
            this.logger.LogWarning($"Rejecting login: Request object not valid. ModelState: {this.ModelState}");
            return this.BadRequest(this.ModelState);
        }

        var response = await this.userService.AuthenticateAsync(request);
        if (response == null)
        {
            this.logger.LogWarning("Rejecting login: Invalid credentials.");
            return this.Unauthorized(new { message = "Invalid credentials" });
        }

        return this.Ok(response);
    }

    /// <summary>
    /// change password request.
    /// </summary>
    /// <param name="request"> current and new and password. </param>
    /// <returns>Upon successful request, gets a jwt token. </returns>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (!this.ModelState.IsValid)
        {
            this.logger.LogWarning($"Password change rejected: Request object not valid. ModelState: {this.ModelState}");
            return this.BadRequest(this.ModelState);
        }

        var resp = await this.userService.ChangePasswordAsync(request.CurrentPassword, request.NewPassword);
        if (!resp.Success)
        {
            return this.BadRequest(resp.ErrorMessage);
        }

        return this.Ok("Password changed successfully");
    }

    /// <summary>
    /// Verifies a user's identity and contact information.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="verificationRequest">The verification request details.</param>
    /// <returns>Activation response or error details.</returns>
    [HttpPost("{userId}/verify")]
    public async Task<IActionResult> VerifyUser([FromRoute] Guid userId, [FromBody] VerificationRequest verificationRequest)
    {
        if (!this.ModelState.IsValid)
        {
            this.logger.LogWarning($"Rejecting user verification: Request object not valid. ModelState: {this.ModelState}");
            return this.BadRequest(this.ModelState);
        }

        var response = await this.userService.ActivateUser(userId, verificationRequest);
        return this.Ok(response);
    }

    /// <summary>
    /// Gets a list of users available for work, filtered by dialect, Elo rating, workload, and limit.
    /// </summary>
    /// <param name="dialect">Dialect filter.</param>
    /// <param name="minElo">Minimum Elo rating.</param>
    /// <param name="maxElo">Maximum Elo rating.</param>
    /// <param name="maxWorkload">Maximum workload.</param>
    /// <param name="limit">Maximum number of users to return.</param>
    /// <returns>List of available users and query metadata.</returns>
    [HttpGet("available-for-work")]
    public async Task<IActionResult> GetAvailableForWork(
        [FromQuery] string? dialect,
        [FromQuery] int? minElo,
        [FromQuery] int? maxElo,
        [FromQuery] int? maxWorkload,
        [FromQuery] int? limit)
    {
        if (!this.ModelState.IsValid)
        {
            this.logger.LogWarning($"Rejecting available users check: Request object not valid. ModelState: {this.ModelState}");
            return this.BadRequest(this.ModelState);
        }

        var resp = await this.userService.GetUserAvailabilitySummaryAsync(dialect, minElo, maxElo, maxWorkload, limit);
        return this.Ok(resp);
    }

    /// <summary>
    /// Gets the availability status of a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>User availability details or not found message.</returns>
    [HttpGet("{userId}/availability")]
    public async Task<IActionResult> GetAvailability([FromRoute] Guid userId)
    {
        if (!this.ModelState.IsValid)
        {
            this.logger.LogWarning($"Rejecting availability check: Request object not valid. ModelState: {this.ModelState}");
            return this.BadRequest(this.ModelState);
        }

        var availability = await this.userService.GetAvailabilityAsync(userId);
        return this.Ok(availability == null ? "User availability Not Found" : availability);
    }

    /// <summary>
    /// Updates the availability status of a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="availabilityUpdate">The availability update request.</param>
    /// <returns>Update response or error details.</returns>
    [HttpPatch("{userId}/availability")]
    public async Task<IActionResult> PatchAvailability([FromRoute] Guid userId, [FromBody] UserAvailabilityUpdateRequest availabilityUpdate)
    {
        if (!this.ModelState.IsValid)
        {
            this.logger.LogWarning($"Rejecting availability update: Request object not valid. ModelState: {this.ModelState}");
            return this.BadRequest(this.ModelState);
        }

        var resp = await this.userService.PatchAvailabilityAsync(userId, availabilityUpdate);
        return this.Ok(resp);
    }

    /// <summary>
    /// Gets the profile information of a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>User profile details or error information.</returns>
    [HttpGet("{userId}/profile")]
    public async Task<IActionResult> GetProfile([FromRoute] Guid userId)
    {
        if (!this.ModelState.IsValid)
        {
            this.logger.LogWarning($"Rejecting profile get: Request object not valid. ModelState: {this.ModelState}");
            return this.BadRequest(this.ModelState);
        }

        var profile = await this.userService.GetProfileAsync(userId);
        return this.Ok(profile);
    }

    /// <summary>
    /// Gets the Elo history for a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>Elo history details or error information.</returns>
    [HttpGet("{userId}/elo-history")]
    public async Task<IActionResult> GetEloHistory([FromRoute] Guid userId)
    {
        var profile = await this.eloService.GetEloHistoryAsync(userId);
        return this.Ok(profile);
    }

    /// <summary>
    /// Claims a job for a user if eligible.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="claimJobRequest">The job claim request details.</param>
    /// <returns>Claim response or error details.</returns>
    [HttpPost("{userId}/claim-job")]
    public async Task<IActionResult> ClaimJob([FromRoute] Guid userId, [FromBody] ClaimJobRequest claimJobRequest)
    {
        if (!this.ModelState.IsValid)
        {
            this.logger.LogWarning($"Rejecting job claim: Request object not valid. ModelState: {this.ModelState}");
            return this.BadRequest(this.ModelState);
        }

        var resp = await this.userService.ClaimJobAsync(userId, claimJobRequest);
        return this.Ok(resp);
    }

    /// <summary>
    /// Validates a tiebreaker claim for a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="tiebreakerRequest">The tiebreaker claim request details.</param>
    /// <returns>Validation response or error details.</returns>
    [HttpPost("{userId}/validate-tiebreaker-claim")]
    public async Task<IActionResult> ValidateTiebreakerClaim([FromRoute] Guid userId, [FromBody] ValidateTiebreakerClaimRequest tiebreakerRequest)
    {
        if (!this.ModelState.IsValid)
        {
            this.logger.LogWarning($"Rejecting tiebreaker claim: Request object not valid. ModelState: {this.ModelState}");
            return this.BadRequest(this.ModelState);
        }

        var resp = await this.userService.ValidateTieBreakerClaim(userId, tiebreakerRequest);
        return this.Ok(resp);
    }

    /// <summary>
    /// Gets the professional status of a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>Professional status response or error details.</returns>
    [HttpGet("{userId}/professional-status")]
    public async Task<IActionResult> GetProfessionalStatus([FromRoute] Guid userId)
    {
        var resp = await this.userService.GetProfessionalStatus(userId);
        return this.Ok(resp);
    }

    /// <summary>
    /// Checks the professional status for a batch of users.
    /// </summary>
    /// <param name="userIds">The batch request object.</param>
    /// <returns>Batch check result summary.</returns>
    [HttpPost("check-professional-status")]
    public async Task<IActionResult> BatchCheckProfessionalStatus([FromBody] List<Guid> userIds)
    {
        var resp = await this.userService.GetBatchProfessionalStatus(userIds);
        return this.Ok(resp);
    }
}

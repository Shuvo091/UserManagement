using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SharedLibrary.AppEnums;
using SharedLibrary.RequestResponseModels.UserManagement;

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
    private readonly IVerificationRequirementService verificationRequirementService;
    private readonly IRedisService redisService;
    private readonly int defaultBookoutInMinutes;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<UsersController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="userService">Service that handles user-related operations such as registration, updates, and retrieval.</param>
    /// <param name="eloService">Service that manages Elo rating calculations and history.</param>
    /// <param name="redisService">Service for managing Redis-based caching and availability tracking.</param>
    /// <param name="appContantOptions">Application configuration used to access settings.</param>
    /// <param name="serviceScopeFactory">Factory for creating service scopes, used for resolving scoped services inside background tasks.</param>
    /// <param name="verificationRequirementService">Service to manage and retrieve verification requirements and policies.</param>
    /// <param name="logger"> logger. </param>
    public UsersController(
        IUserService userService,
        IEloService eloService,
        IRedisService redisService,
        IOptions<AppConstantsOptions> appContantOptions,
        IServiceScopeFactory serviceScopeFactory,
        IVerificationRequirementService verificationRequirementService,
        ILogger<UsersController> logger)
    {
        this.userService = userService;
        this.eloService = eloService;
        this.verificationRequirementService = verificationRequirementService;
        this.redisService = redisService;
        var defaultBookout = appContantOptions.Value.DefaultBookoutMinutes;

        this.defaultBookoutInMinutes = defaultBookout;
        this.serviceScopeFactory = serviceScopeFactory;
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
            return this.BadRequest(this.ModelState);
        }

        if (!dto.ConsentToDataProcessing)
        {
            return this.BadRequest("Consent to data processing needed!");
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
        var response = await this.userService.AuthenticateAsync(request);
        if (response == null)
        {
            return this.Unauthorized(new { message = "Invalid credentials" });
        }

        return this.Ok(response);
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
            return this.BadRequest(this.ModelState);
        }

        var requirements = await this.verificationRequirementService.GetVerificationRequirement();
        if (requirements is null)
        {
            return this.BadRequest(new { error = "Verification requirements not configured." });
        }

        var user = await this.userService.GetUserAsync(userId);
        if (user is null)
        {
            return this.NotFound(new { error = "User not found." });
        }

        // Type check
        if (requirements.RequireIdDocument &&
            verificationRequest.VerificationType != VerificationType.IdDocument.ToDisplayName())
        {
            return this.BadRequest(new { error = "Verification type must be 'IdDocument'." });
        }

        // ID Document check
        var idValidation = verificationRequest.IdDocumentValidation;
        if (requirements.RequireIdDocument)
        {
            if (idValidation is null || !idValidation.Enabled)
            {
                return this.BadRequest(new { error = "ID document validation must be enabled." });
            }

            var validation = idValidation.ValidationResult;
            if (validation is null ||
                !validation.IdFormatValid ||
                !validation.PhotoPresent ||
                !idValidation.PhotoUploaded)
            {
                return this.BadRequest(new { error = "ID document validation failed field checks." });
            }
        }

        // Phone & Email check
        var additional = verificationRequest.AdditionalVerification;

        if (requirements.RequirePhoneVerification && (additional is null || !additional.PhoneVerification))
        {
            return this.BadRequest(new { error = "Phone verification is required." });
        }

        if (requirements.RequireEmailVerification && (additional is null || !additional.EmailVerification))
        {
            return this.BadRequest(new { error = "Email verification is required." });
        }

        // Activate User
        var response = await this.userService.ActivateUser(user, verificationRequest);
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
            return this.BadRequest(this.ModelState);
        }

        var availableUsersResp = new List<UserAvailabilityResponse>();
        var users = await this.userService.GetFilteredUser(dialect, minElo, maxElo, maxWorkload, limit);
        if (!users.Any())
        {
            return this.Ok(availableUsersResp);
        }

        var cacheMap = await this.redisService.GetBulkAvailabilityAsync(users.Select(u => u.Id));
        var trendMap = await this.eloService.BulkEloTrendAsync(users.Select(u => u.Id).ToList(), 7);
        var availableUsers = users
            .Where(u => cacheMap.ContainsKey(u.Id)
                    && cacheMap[u.Id].Status == UserAvailabilityType.Available.ToDisplayName())
            .Select(u =>
            {
                var availability = cacheMap.ContainsKey(u.Id)
                    ? cacheMap[u.Id]
                    : null;

                return new AvailableUsersDto
                {
                    UserId = u.Id,
                    EloRating = u.Statistics?.CurrentElo,
                    PeakElo = u.Statistics?.PeakElo,
                    DialectExpertise = u.Dialects.Select(d => d.Dialect).ToList(),
                    CurrentWorkload = availability?.CurrentWorkload,
                    RecentPerformance = trendMap[u.Id],
                    GamesPlayed = u.Statistics?.GamesPlayed,
                    Role = u.Role,
                    BypassQaComparison = u.Role == UserRoleType.Professional.ToDisplayName(),
                    LastActive = u.Statistics?.LastCalculated,
                };
            })
            .ToList();

        return this.Ok(new UserAvailabilityResponse
        {
            AvailableUsers = availableUsers,
            TotalAvailable = availableUsers.Count,
            QueryTimestamp = DateTime.UtcNow,
        });
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
            return this.BadRequest(this.ModelState);
        }

        var availability = await this.redisService.GetAvailabilityAsync(userId);
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
            return this.BadRequest(this.ModelState);
        }

        var ipAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = this.Request.Headers["User-Agent"].ToString() ?? "unknown";
        var existingAvailability = await this.redisService.GetAvailabilityAsync(userId)
                            ?? new UserAvailabilityRedisDto();

        if (!EnumDisplayHelper.TryParseDisplayName(availabilityUpdate.Status, out UserAvailabilityType outcome))
        {
            return this.BadRequest("Invalid Status Provided.");
        }

        if (availabilityUpdate.MaxConcurrentJobs < 1)
        {
            return this.BadRequest("Maximum concurrent job should be greater than 0");
        }

        if (availabilityUpdate != null)
        {
            existingAvailability.Status = availabilityUpdate.Status;
            existingAvailability.MaxConcurrentJobs = availabilityUpdate.MaxConcurrentJobs;
        }

        existingAvailability.LastUpdate = DateTime.UtcNow;

        // 1. Write to Redis
        await this.redisService.SetAvailabilityAsync(userId, existingAvailability);

        // 2. Async sync to PostgreSQL for audit (simplified placeholder here)
        _ = Task.Run(async () =>
        {
            await this.userService.UpdateAvailabilityAuditAsync(userId, existingAvailability, ipAddress, userAgent);
        });

        return this.Ok(new UserAvailabilityUpdateResponse
        {
            AvailabilityUpdated = "success",
            CurrentStatus = existingAvailability.Status,
            MaxConcurrentJobs = existingAvailability.MaxConcurrentJobs,
            LastUpdated = existingAvailability.LastUpdate,
        });
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
        if (!this.ModelState.IsValid)
        {
            return this.BadRequest(this.ModelState);
        }

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
            return this.BadRequest(this.ModelState);
        }

        var availability = await this.redisService.GetAvailabilityAsync(userId);

        if (availability == null || availability.Status != UserAvailabilityType.Available.ToDisplayName())
        {
            return this.BadRequest(new { error = "User is currently unavailable for work." });
        }

        if (availability.CurrentWorkload >= availability.MaxConcurrentJobs)
        {
            return this.BadRequest(new { error = "User already has maximum concurrent jobs." });
        }

        var tryLockJobClaim = await this.redisService.TryClaimJobAsync(claimJobRequest.JobId, userId);
        if (!tryLockJobClaim)
        {
            return this.BadRequest(new { error = "Job is already claimed by another user." });
        }

        availability.CurrentWorkload++;
        await this.redisService.AddUserClaimAsync(userId, claimJobRequest.JobId);
        await this.redisService.SetAvailabilityAsync(userId, availability);
        var claimId = Guid.NewGuid();
        var response = new ClaimJobResponse
        {
            ClaimValidated = true,
            UserEligible = true,
            ClaimId = claimId,
            UserAvailability = availability.Status,
            CurrentWorkload = availability.CurrentWorkload,
            MaxConcurrentJobs = availability.MaxConcurrentJobs,
            CapacityReservedUntil = DateTime.UtcNow.AddMinutes(this.defaultBookoutInMinutes),
        };

        // Fire-and-forget task
        _ = Task.Run(async () =>
        {
            using var scope = this.serviceScopeFactory.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            await userService.ClaimJobAsync(userId, claimId, claimJobRequest, response.CapacityReservedUntil);
        });

        await this.redisService.ReleaseJobClaimAsync(claimJobRequest.JobId);

        return this.Ok(response);
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
            return this.BadRequest(this.ModelState);
        }

        var profile = await this.userService.ValidateTieBreakerClaim(userId, tiebreakerRequest);
        if (profile.IsOriginalTranscriber)
        {
            return this.Forbid("Users who participated in the original transcription cannot be tiebreakers");
        }

        if (!profile.UserEloQualified)
        {
            return this.Forbid("Users does not meet minimal elo requirement");
        }

        return this.Ok(profile);
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

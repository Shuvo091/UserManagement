using CohesionX.UserManagement.Application.Interfaces;
using CohesionX.UserManagement.Application.Models;
using CohesionX.UserManagement.Application.Services;
using CohesionX.UserManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SharedLibrary.AppEnums;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Controllers
{
	/// <summary>
	/// API controller for user management operations such as registration, verification, availability, and job claiming.
	/// </summary>
	#if !DEBUG || !SKIP_AUTH
	[Authorize]
	#endif
	[ApiController]
	[Route("api/v1/users")]
	public class UsersController : ControllerBase
	{
		private readonly IUserService _userService;
		private readonly IEloService _eloService;
		private readonly IVerificationRequirementService _verificationRequirementService;
		private readonly IRedisService _redisService;
		private readonly int _defaultBookoutInMinutes;
		private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly ILogger<UsersController> _logger;

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
			_userService = userService;
			_eloService = eloService;
			_verificationRequirementService = verificationRequirementService;
			_redisService = redisService;
			var defaultBookout = appContantOptions.Value.DefaultBookoutMinutes;

			_defaultBookoutInMinutes = defaultBookout;
			_serviceScopeFactory = serviceScopeFactory;
			_logger = logger;
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
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (!dto.ConsentToDataProcessing)
			{
				return BadRequest("Consent to data processing needed!");
			}

			var result = await _userService.RegisterUserAsync(dto);

			return CreatedAtAction(nameof(GetProfile), new { userId = result.UserId }, result);
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
			var requirements = await _verificationRequirementService.GetVerificationRequirement();
			if (requirements is null)
			{
				return BadRequest(new { error = "Verification requirements not configured." });
			}

			var user = await _userService.GetUserAsync(userId);
			if (user is null)
			{
				return NotFound(new { error = "User not found." });
			}

			// Type check
			if (requirements.RequireIdDocument &&
				verificationRequest.VerificationType != VerificationType.IdDocument.ToDisplayName())
			{
				return BadRequest(new { error = "Verification type must be 'IdDocument'." });
			}

			// ID Document check
			var idValidation = verificationRequest.IdDocumentValidation;
			if (requirements.RequireIdDocument)
			{
				if (idValidation is null || !idValidation.Enabled)
				{
					return BadRequest(new { error = "ID document validation must be enabled." });
				}

				var validation = idValidation.ValidationResult;
				if (validation is null ||
					!validation.IdFormatValid ||
					!validation.PhotoPresent ||
					!idValidation.PhotoUploaded)
				{
					return BadRequest(new { error = "ID document validation failed field checks." });
				}
			}

			// Phone & Email check
			var additional = verificationRequest.AdditionalVerification;

			if (requirements.RequirePhoneVerification && (additional is null || !additional.PhoneVerification))
			{
				return BadRequest(new { error = "Phone verification is required." });
			}

			if (requirements.RequireEmailVerification && (additional is null || !additional.EmailVerification))
			{
				return BadRequest(new { error = "Email verification is required." });
			}

			// Activate User
			var response = await _userService.ActivateUser(user, verificationRequest);
			return Ok(response);
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
			var availableUsersResp = new List<UserAvailabilityResponse>();
			var users = await _userService.GetFilteredUser(dialect, minElo, maxElo, maxWorkload, limit);
			if (!users.Any())
			{
				return Ok(availableUsersResp);
			}

			var cacheMap = await _redisService.GetBulkAvailabilityAsync(users.Select(u => u.Id));
			var trendMap = await _eloService.BulkEloTrendAsync(users.Select(u => u.Id).ToList(), 7);
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

			return Ok(new UserAvailabilityResponse
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
			var availability = await _redisService.GetAvailabilityAsync(userId);
			return Ok(availability == null ? "User availability Not Found" : availability);
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
			var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
			var userAgent = Request.Headers["User-Agent"].ToString() ?? "unknown";
			var existingAvailability = await _redisService.GetAvailabilityAsync(userId)
								?? new UserAvailabilityRedisDto();

			if (!EnumDisplayHelper.TryParseDisplayName(availabilityUpdate.Status, out UserAvailabilityType outcome))
			{
				return BadRequest("Invalid Status Provided.");
			}

			if (availabilityUpdate.MaxConcurrentJobs < 1)
			{
				return BadRequest("Maximum concurrent job should be greater than 0");
			}

			if (availabilityUpdate != null)
			{
				existingAvailability.Status = availabilityUpdate.Status;
				existingAvailability.MaxConcurrentJobs = availabilityUpdate.MaxConcurrentJobs;
			}

			existingAvailability.LastUpdate = DateTime.UtcNow;

			// 1. Write to Redis
			await _redisService.SetAvailabilityAsync(userId, existingAvailability);

			// 2. Async sync to PostgreSQL for audit (simplified placeholder here)
			_ = Task.Run(async () =>
			{
				await _userService.UpdateAvailabilityAuditAsync(userId, existingAvailability, ipAddress, userAgent);
			});

			return Ok(new UserAvailabilityUpdateResponse
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
			var profile = await _userService.GetProfileAsync(userId);
			return Ok(profile);
		}

		/// <summary>
		/// Gets the Elo history for a user.
		/// </summary>
		/// <param name="userId">The user's unique identifier.</param>
		/// <returns>Elo history details or error information.</returns>
		[HttpGet("{userId}/elo-history")]
		public async Task<IActionResult> GetEloHistory([FromRoute] Guid userId)
		{
			var profile = await _eloService.GetEloHistoryAsync(userId);
			return Ok(profile);
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
			var availability = await _redisService.GetAvailabilityAsync(userId);

			if (availability == null || availability.Status != UserAvailabilityType.Available.ToDisplayName())
			{
				return BadRequest(new { error = "User is currently unavailable for work." });
			}

			if (availability.CurrentWorkload >= availability.MaxConcurrentJobs)
			{
				return BadRequest(new { error = "User already has maximum concurrent jobs." });
			}

			var tryLockJobClaim = await _redisService.TryClaimJobAsync(claimJobRequest.JobId, userId);
			if (!tryLockJobClaim)
			{
				return BadRequest(new { error = "Job is already claimed by another user." });
			}

			availability.CurrentWorkload++;
			await _redisService.AddUserClaimAsync(userId, claimJobRequest.JobId);
			await _redisService.SetAvailabilityAsync(userId, availability);
			var claimId = Guid.NewGuid();
			var response = new ClaimJobResponse
			{
				ClaimValidated = true,
				UserEligible = true,
				ClaimId = claimId,
				UserAvailability = availability.Status,
				CurrentWorkload = availability.CurrentWorkload,
				MaxConcurrentJobs = availability.MaxConcurrentJobs,
				CapacityReservedUntil = DateTime.UtcNow.AddMinutes(_defaultBookoutInMinutes),
			};

			// Fire-and-forget task
			_ = Task.Run(async () =>
			{
				using var scope = _serviceScopeFactory.CreateScope();
				var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

				await userService.ClaimJobAsync(userId, claimId, claimJobRequest, response.CapacityReservedUntil);
			});

			await _redisService.ReleaseJobClaimAsync(claimJobRequest.JobId);

			return Ok(response);
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
			var profile = await _userService.ValidateTieBreakerClaim(userId, tiebreakerRequest);
			if (profile.IsOriginalTranscriber)
			{
				return Forbid("Users who participated in the original transcription cannot be tiebreakers");
			}

			if (!profile.UserEloQualified)
			{
				return Forbid("Users does not meet minimal elo requirement");
			}

			return Ok(profile);
		}

		/// <summary>
		/// Gets the professional status of a user.
		/// </summary>
		/// <param name="userId">The user's unique identifier.</param>
		/// <returns>Professional status response or error details.</returns>
		[HttpGet("{userId}/professional-status")]
		public async Task<IActionResult> GetProfessionalStatus([FromRoute] Guid userId)
		{
			var status = await _userService.GetProfessionalStatus(userId);
			return Ok(status);
		}

		/// <summary>
		/// Checks the professional status for a batch of users.
		/// </summary>
		/// <param name="batchRequest">The batch request object.</param>
		/// <returns>Batch check result summary.</returns>
		[HttpPost("check-professional-status")]
		public IActionResult BatchCheckProfessionalStatus([FromBody] object batchRequest)
		{
			throw new NotImplementedException("not yet been implemented.");
		}
	}
}

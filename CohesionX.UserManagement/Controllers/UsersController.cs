using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SharedLibrary.RequestResponseModels.UserManagement;
using SharedLibrary.AppEnums;

namespace CohesionX.UserManagement.Controllers
{
	//[Authorize(Policy = "ApiScope")]
	[ApiController]
	[Route("api/v1/users")]
	[IgnoreAntiforgeryToken]
	public class UsersController : ControllerBase
	{
		private readonly IUserService _userService;
		private readonly IEloService _eloService;
		private readonly IRedisService _redisService;
		private readonly int _defaultBookoutInMinutes;
		private readonly IServiceScopeFactory _serviceScopeFactory;


		public UsersController(IUserService userService
			, IEloService eloService
			, IRedisService redisService
			, IConfiguration configuration
			, IServiceScopeFactory serviceScopeFactory)
		{
			_userService = userService;
			_eloService = eloService;
			_redisService = redisService;
			var defaultBookoutStr = configuration["DEFAULT_BOOKOUT_MINUTES"];
			if (!int.TryParse(defaultBookoutStr, out var defaultBookout)) defaultBookout = 480;
			_defaultBookoutInMinutes = defaultBookout;
			_serviceScopeFactory = serviceScopeFactory;
		}

		[AllowAnonymous]
		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] UserRegisterRequest dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _userService.RegisterUserAsync(dto);

			return CreatedAtAction(nameof(GetProfile), new { userId = result.UserId }, result);
		}

		[HttpPost("{userId}/verify")]
		public async Task<IActionResult> VerifyUser([FromRoute] Guid userId, [FromBody] VerificationRequest verificationRequest)
		{
			var user = await _userService.GetUserAsync(userId);
			if(user is null)
			{
				return NotFound(new { error = "User not found" });
			}
			// Basic validation checks
			if (verificationRequest.VerificationType != VerificationType.IdDocument.ToDisplayName())
			{
				return BadRequest(new { error = "Unsupported verification type" });
			}

			var idValidation = verificationRequest.IdDocumentValidation;
			if (idValidation is null || !idValidation.Enabled)
			{
				return BadRequest(new { error = "ID document validation must be enabled" });
			}

			var validationResult = idValidation.ValidationResult;
			if (validationResult is null ||
				!validationResult.IdFormatValid ||
				!validationResult.PhotoPresent ||
				!idValidation.PhotoUploaded ||
				user.IdNumber != idValidation.IdNumber )
			{
				return BadRequest(new { error = "ID document validation failed field checks" });
			}

			var additional = verificationRequest.AdditionalVerification;
			if (additional is null ||
				!additional.PhoneVerification ||
				!additional.EmailVerification)
			{
				return BadRequest(new { error = "Phone and Email verification must be completed" });
			}

			var response = await _userService.ActivateUser(user, verificationRequest);
			return Ok(response);
		}

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
			if (!users.Any()) return Ok(availableUsersResp);

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
						LastActive = u.Statistics?.LastCalculated
					};
				})
				.ToList();


			return Ok(new UserAvailabilityResponse { 
				AvailableUsers = availableUsers,
				TotalAvailable = availableUsers.Count,
				QueryTimestamp = DateTime.UtcNow
			});
		}

		[HttpGet("{userId}/availability")]
		public async Task<IActionResult> GetAvailability([FromRoute] Guid userId)
		{

			var availability = await _redisService.GetAvailabilityAsync(userId);
			return Ok(availability == null ? "User Not Found" : availability);
		}

		[HttpPatch("{userId}/availability")]
		public async Task<IActionResult> PatchAvailability([FromRoute] Guid userId, [FromBody] UserAvailabilityUpdateRequest availabilityUpdate)
		{
			var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
			var userAgent = Request.Headers["User-Agent"].ToString() ?? "unknown";
			var existingAvailability = await _redisService.GetAvailabilityAsync(userId)
								?? new UserAvailabilityRedisDto();

			if (!EnumDisplayHelper.TryParseDisplayName(availabilityUpdate.Status, out UserAvailabilityType outcome)) return BadRequest("Invalid Status Provided.");
			if (availabilityUpdate.MaxConcurrentJobs < 1) return BadRequest("Maximum concurrent job should be greater than 0");
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
				LastUpdated = existingAvailability.LastUpdate
			});
		}

		[HttpGet("{userId}/profile")]
		public async Task<IActionResult> GetProfile([FromRoute] Guid userId)
		{
			try
			{
				var profile = await _userService.GetProfileAsync(userId);
				return Ok(profile);
			}
			catch(Exception e)
			{
				return StatusCode(500, new { error = "An error occurred while processing your request." });

			}
		}

		[HttpGet("{userId}/elo-history")]
		public async Task<IActionResult> GetEloHistory([FromRoute] Guid userId)
		{
			try
			{
				var profile = await _eloService.GetEloHistoryAsync(userId);
				return Ok(profile);
			}
			catch (Exception e)
			{
				return StatusCode(500, new { error = "An error occurred while processing your request." });

			}
		}

		[HttpPost("{userId}/claim-job")]
		public async Task<IActionResult> ClaimJob([FromRoute] Guid userId, [FromBody] ClaimJobRequest claimJobRequest)
		{
			try
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
					CapacityReservedUntil = DateTime.UtcNow.AddMinutes(_defaultBookoutInMinutes)
				};

				// Fire-and-forget task
				_ = Task.Run(async () =>
				{
					using var scope = _serviceScopeFactory.CreateScope();
					var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

					try
					{
						await userService.ClaimJobAsync(userId, claimId, claimJobRequest, response.CapacityReservedUntil);
					}
					catch (Exception ex)
					{
						//_logger.LogError(ex, "Error in ClaimJobAsync for UserId: {UserId}", userId);
					}
				});


				await _redisService.ReleaseJobClaimAsync(claimJobRequest.JobId);

				return Ok(response);
			}
			catch (Exception)
			{
				return StatusCode(500, new { error = "An error occurred while processing your request." });
			}
		}

		[HttpPost("{userId}/validate-tiebreaker-claim")]
		public IActionResult ValidateTiebreakerClaim([FromRoute] Guid userId, [FromBody] object tiebreakerRequest)
		{
			// TODO: Implement tiebreaker claim logic
			return Ok(new
			{
				tiebreakerClaimValidated = true,
				userId,
				userEloQualified = true,
				currentElo = 1420,
				isOriginalTranscriber = false,
				claimId = Guid.NewGuid(),
				bonusConfirmed = true,
				estimatedCompletion = "45m"
			});
		}

		[HttpGet("{userId}/professional-status")]
		public IActionResult GetProfessionalStatus([FromRoute] Guid userId)
		{
			// TODO: Implement get professional status logic
			return Ok(new
			{
				userId,
				isProfessional = false,
				currentRole = "transcriber",
				currentElo = 1367,
				eligibleForProfessional = true,
				eligibilityCriteria = new { minEloRequired = 1600, minJobsRequired = 500, userElo = 1367, userJobs = 247 }
			});
		}

		[HttpPost("check-professional-status")]
		public IActionResult BatchCheckProfessionalStatus([FromBody] object batchRequest)
		{
			// TODO: Implement batch check logic
			return Ok(new
			{
				professionalStatuses = new { },
				summary = new { totalChecked = 0, professionals = 0, standardTranscribers = 0 }
			});
		}
	}
}
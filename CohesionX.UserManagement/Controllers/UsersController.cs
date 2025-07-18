using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CohesionX.UserManagement.Modules.Users.Domain.Constants;
using CohesionX.UserManagement.Modules.Users.Application.Enums;
using AutoMapper;

namespace CohesionX.UserManagement.Controllers
{
	[ApiController]
	[Route("api/v1/users")]
	[IgnoreAntiforgeryToken]
	public class UsersController : ControllerBase
	{
		private readonly IUserService _userService;
		private readonly IFileStorageService _fileStorageService;
		private readonly IRedisService _redisService;
		private readonly IMapper _mapper;


		public UsersController(IUserService userService
			, IFileStorageService fileStorageService
			, IRedisService redisService
			, IMapper mapper)
		{
			_userService = userService;
			_fileStorageService = fileStorageService;
			_redisService = redisService;
			_mapper = mapper;
		}

		[HttpPost("register")]
		public async Task<IActionResult> Register([FromForm] UserRegisterRequest dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _userService.RegisterUserAsync(dto);

			return Created($"/api/v1/users/{result.UserId}/profile", result);
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
			if (verificationRequest.VerificationType != "id_document")
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

			var cacheMap = await _redisService.GetBulkAvailabilityAndEloAsync(users.Select(u => u.Id));

			var availableUsers = users
				.Where(u => cacheMap.AvailabilityMap.ContainsKey(u.Id)
						&& cacheMap.AvailabilityMap[u.Id].Status.ToLower() == UserAvailabilityType.AVAILABLE.ToLower())
				.Select(u =>
				{
					var availability = cacheMap.AvailabilityMap.ContainsKey(u.Id)
						? cacheMap.AvailabilityMap[u.Id]
						: null;

					var eloPerformance = cacheMap.EloMap.ContainsKey(u.Id)
						? cacheMap.EloMap[u.Id]
						: null;

					return new AvailableUsersDto
					{
						UserId = u.Id,
						EloRating = eloPerformance?.CurrentElo,
						PeakElo = eloPerformance?.PeakElo,
						DialectExpertise = u.Dialects.Select(d => d.Dialect).ToList(),
						CurrentWorkload = availability?.CurrentWorkload,
						RecentPerformance = eloPerformance?.RecentTrend,
						GamesPlayed = eloPerformance?.GamesPlayed,
						Role = u.Role,
						BypassQaComparison = u.Role == UserRole.PROFESSIONAL,
						LastActive = eloPerformance?.LastJobCompleted
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
			// TODO: Replace with actual service call
			var profile = await _userService.GetProfileAsync(userId);
			return Ok(profile);
		}

		[HttpGet("{userId}/elo-history")]
		public IActionResult GetEloHistory([FromRoute] Guid userId)
		{
			// TODO: Implement elo history logic
			return Ok(new
			{
				userId,
				currentElo = 1200,
				peakElo = 1200,
				initialElo = 1200,
				gamesPlayed = 0,
				eloHistory = new object[] { },
				trends = new { last7Days = "+0", last30Days = "+0", winRate = 0.0, averageOpponentElo = 1200 }
			});
		}

		[HttpPost("{userId}/claim-job")]
		public IActionResult ClaimJob([FromRoute] Guid userId, [FromBody] object claimJobRequest)
		{
			// TODO: Implement claim job logic
			return Ok(new
			{
				claimValidated = true,
				userEligible = true,
				claimId = Guid.NewGuid(),
				userAvailability = "available",
				isProfessional = false,
				bypassQARequired = false,
				currentWorkload = 1,
				maxConcurrentJobs = 3,
				capacityReservedUntil = DateTime.UtcNow.AddHours(2)
			});
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
using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using CohesionX.UserManagement.Shared.Persistence;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using System.Text.Json;

namespace CohesionX.UserManagement.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [IgnoreAntiforgeryToken]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IDistributedCache _cache;
        private readonly AppDbContext _db;

        public UsersController(IUserService userService, IFileStorageService fileStorageService, IDistributedCache cache, AppDbContext db)
        {
            _userService = userService;
            _fileStorageService = fileStorageService;
            _cache = cache;
            _db = db;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] UserRegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string? idPhotoPath = null;
            if (dto.IdPhoto != null)
            {
                idPhotoPath = await _fileStorageService.StoreFileAsync(dto.IdPhoto);
            }

            var result = await _userService.RegisterUserAsync(dto, idPhotoPath);

            return Created($"/api/v1/users/{result.UserId}/profile", new
            {
                userId = result.UserId,
                eloRating = 1200,
                status = result.Status,
                profileUri = $"/api/v1/users/{result.UserId}/profile",
                verificationRequired = result.VerificationRequired
            });
        }

        [HttpPost("{userId}/verify")]
        public IActionResult VerifyUser([FromRoute] Guid userId, [FromBody] object verificationRequest)
        {
            // TODO: Implement verification logic
            return Ok(new
            {
                verificationStatus = "approved",
                eloRating = 1200,
                statusChanged = "pending_verification -> active",
                eligibleForWork = true,
                activationMethod = "automatic",
                activatedAt = DateTime.UtcNow,
                verificationLevel = "basic_v1",
                nextSteps = new[] { "profile_completion", "job_browsing" },
                roadmapNote = "V2 will include automated ID verification via Department of Home Affairs"
            });
        }

        [HttpGet("available-for-work")]
        public async Task<IActionResult> GetAvailableForWork([FromQuery] string? dialect, [FromQuery] int? minElo, [FromQuery] int? maxElo, [FromQuery] int? maxWorkload, [FromQuery] int? limit)
        {
            // Get all users (could be optimized for large datasets)
            var users = _db.Users
                .Where(u => u.IsActive && u.Status == "active")
                .Select(u => new { u.Id, u.EloRating, u.PeakElo, u.IsProfessional, u.UserRole, u.GamesPlayed })
                .ToList();

            var availableUsers = new List<object>();
            int count = 0;
            foreach (var user in users)
            {
                // Read availability from Redis
                var cacheKey = $"user:availability:{user.Id}";
                string? availabilityJson = await _cache.GetStringAsync(cacheKey);
                if (availabilityJson == null) continue;
                var availability = JsonSerializer.Deserialize<UserAvailabilityDto>(availabilityJson);
                if (availability == null || availability.Status != "available") continue;

                // Filter by maxWorkload
                if (maxWorkload.HasValue && availability.CurrentWorkload > maxWorkload.Value) continue;

                // Get dialects from DB
                var userDialects = _db.UserDialects.Where(d => d.UserId == user.Id).Select(d => d.DialectCode).ToList();
                if (!string.IsNullOrEmpty(dialect) && !userDialects.Contains(dialect)) continue;

                // Filter by Elo
                if (minElo.HasValue && user.EloRating < minElo.Value) continue;
                if (maxElo.HasValue && user.EloRating > maxElo.Value) continue;

                // Compose response
                availableUsers.Add(new
                {
                    userId = user.Id,
                    eloRating = user.EloRating,
                    peakElo = user.PeakElo,
                    dialectExpertise = userDialects,
                    currentWorkload = availability.CurrentWorkload,
                    recentPerformance = "+0_over_7_days", // TODO: Calculate from EloHistory
                    gamesPlayed = user.GamesPlayed,
                    role = user.IsProfessional ? "professional" : user.UserRole.ToString().ToLower(),
                    bypassQaComparison = user.IsProfessional,
                    lastActive = availability.LastUpdate
                });
                count++;
                if (limit.HasValue && count >= limit.Value) break;
            }

            return Ok(new
            {
                availableUsers,
                totalAvailable = availableUsers.Count,
                queryTimestamp = DateTime.UtcNow
            });
        }

        [HttpGet("{userId}/availability")]
        public IActionResult GetAvailability([FromRoute] Guid userId)
        {
            // TODO: Implement get availability logic
            return Ok(new
            {
                status = "available",
                maxConcurrentJobs = 3,
                currentWorkload = 1,
                lastUpdate = DateTime.UtcNow
            });
        }

        [HttpPatch("{userId}/availability")]
        public IActionResult PatchAvailability([FromRoute] Guid userId, [FromBody] object availabilityUpdate)
        {
            // TODO: Implement patch availability logic
            return Ok(new
            {
                availabilityUpdated = true,
                currentStatus = "available",
                maxConcurrentJobs = 3,
                lastUpdated = DateTime.UtcNow
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

        private class UserAvailabilityDto
        {
            public string Status { get; set; } = "";
            public int MaxConcurrentJobs { get; set; }
            public int CurrentWorkload { get; set; }
            public DateTime LastUpdate { get; set; }
        }
    }
} 
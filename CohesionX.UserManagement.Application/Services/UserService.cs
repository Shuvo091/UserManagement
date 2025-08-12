// <copyright file="UserService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AutoMapper;
using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Abstractions.Services;
using CohesionX.UserManagement.Database.Abstractions.Entities;
using CohesionX.UserManagement.Database.Abstractions.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.AppEnums;
using SharedLibrary.Common.Options;
using SharedLibrary.Common.Security;
using SharedLibrary.Common.Utilities;
using SharedLibrary.Contracts.Usermanagement.RedisDtos;
using SharedLibrary.Contracts.Usermanagement.Requests;
using SharedLibrary.Contracts.Usermanagement.Responses;

namespace CohesionX.UserManagement.Application.Services;

/// <summary>
/// Provides operations for user registration, profile management, verification, professional status, and job claims.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository repo;
    private readonly IAuditLogRepository auditLogRepo;
    private readonly IJobClaimRepository jobClaimRepo;
    private readonly IEloService eloService;
    private readonly IVerificationRequirementService verificationRequirementService;
    private readonly IRedisService redisService;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly int initElo;
    private readonly int minEloRequiredForPro;
    private readonly int minJobsRequiredForPro;
    private readonly int defaultBookoutInMinutes;
    private readonly ILogger<UserService> logger;
    private readonly IOptions<JwtOptions> jwtOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="repo">Repository for managing user data persistence and retrieval.</param>
    /// <param name="auditLogRepo">Repository for logging user-related audit events and changes.</param>
    /// <param name="jobClaimRepo">Repository for tracking job claim history and status for users.</param>
    /// <param name="redisService">Service for managing Redis-based caching and availability tracking.</param>
    /// <param name="httpContextAccessor"> Http Context accessor.</param>
    /// <param name="mapper">Object mapper used to map between domain entities and DTOs.</param>
    /// <param name="appContantOptions">Application configuration used to retrieve settings and secrets.</param>
    /// <param name="eloService">Service that handles Elo rating logic and updates for users.</param>
    /// <param name="verificationRequirementService">Service to manage and retrieve verification requirements and policies.</param>
    /// <param name="logger"> logger. </param>
    /// <param name="jwtOptions"> the Jwt options from config. </param>
    public UserService(
        IUserRepository repo,
        IAuditLogRepository auditLogRepo,
        IJobClaimRepository jobClaimRepo,
        IRedisService redisService,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IOptions<AppConstantsOptions> appContantOptions,
        IEloService eloService,
        IVerificationRequirementService verificationRequirementService,
        ILogger<UserService> logger,
        IOptions<JwtOptions> jwtOptions)
    {
        this.repo = repo;
        this.auditLogRepo = auditLogRepo;
        this.jobClaimRepo = jobClaimRepo;
        this.eloService = eloService;
        this.verificationRequirementService = verificationRequirementService;
        this.redisService = redisService;
        this.httpContextAccessor = httpContextAccessor;
        var initElo = appContantOptions.Value.InitialEloRating;
        this.initElo = initElo;

        var initMinEloPro = appContantOptions.Value.MinEloRequiredForPro;
        this.minEloRequiredForPro = initMinEloPro;

        var initMinJobsPro = appContantOptions.Value.MinJobsRequiredForPro;
        this.minJobsRequiredForPro = initMinJobsPro;
        this.logger = logger;
        this.jwtOptions = jwtOptions;

        this.defaultBookoutInMinutes = appContantOptions.Value.DefaultBookoutMinutes;
    }

    /// <inheritdoc/>
    public async Task<UserRegisterResponse> RegisterUserAsync(UserRegisterRequest dto)
    {
        if (!dto.ConsentToDataProcessing)
        {
            this.logger.LogWarning("Rejecting registration: Consent to data processing needed!.");
            throw new ArgumentException("Consent to data processing needed!");
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.FirstName) ||
            string.IsNullOrWhiteSpace(dto.LastName) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            this.logger.LogWarning($"Registration failed due to missing fields. Payload: {dto}");
            throw new ArgumentException("All required fields must be provided");
        }

        // Validate password strength
        var passwordError = this.ValidatePassword(dto.Password);
        if (passwordError != null)
        {
            this.logger.LogWarning($"Registration failed due to weak password. Email: {dto.Email}, Reason: {passwordError}");
            throw new ArgumentException(passwordError);
        }

        // Validate South African ID if provided
        if (!this.ValidateSouthAfricanId(dto.IdNumber))
        {
            this.logger.LogWarning($"Registration failed due to invalid South African ID. Email: {dto.Email}, IdNumber: {dto.IdNumber}");
            throw new ArgumentException("Invalid South African ID number");
        }

        // Check if email already exists
        if (await this.repo.EmailExistsAsync(dto.Email))
        {
            this.logger.LogWarning($"Registration failed due email already existing. Email: {dto.Email}");
            throw new ArgumentException("Email already registered");
        }

        // Create user entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            UserName = dto.Email,
            Phone = dto.Phone,
            IdNumber = dto.IdNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = UserStatusType.PendingVerification.ToDisplayName(),
            Role = UserRoleType.Transcriber.ToDisplayName(),
            IsProfessional = false,
        };
        user.PasswordHash = PasswordHasher.Hash(dto.Password);

        // Add dialect preferences
        if (dto.DialectPreferences != null && dto.DialectPreferences.Any())
        {
            foreach (var dialect in dto.DialectPreferences)
            {
                user.Dialects.Add(new UserDialect
                {
                    Dialect = dialect,
                    ProficiencyLevel = dto.LanguageExperience ?? string.Empty,
                    IsPrimary = false,
                    CreatedAt = DateTime.UtcNow,
                });
            }
        }

        user.Statistics = new UserStatistics
        {
            TotalJobs = 0,
            CurrentElo = this.initElo,
            PeakElo = this.initElo,
            GamesPlayed = 0,
            LastCalculated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        // Attempt activation
        var verificationRequired = new List<string>
    {
        "id_document_upload",
    };

        await this.repo.AddAsync(user);
        await this.repo.SaveChangesAsync();

        return new UserRegisterResponse
        {
            UserId = user.Id,
            EloRating = user.Statistics.CurrentElo,
            Status = user.Status,
            ProfileUri = $"/api/v1/users/{user.Id}/profile",
            VerificationRequired = verificationRequired,
        };
    }

    /// <inheritdoc/>
    public async Task<UserProfileResponse> GetProfileAsync(Guid userId)
    {
        var user = await this.repo.GetUserByIdAsync(userId, false, true);
        if (user == null)
        {
            this.logger.LogWarning($"Profile get failed because user with ID {userId} is not found.");
            throw new KeyNotFoundException("User not found");
        }

        var stats = user.Statistics;
        var eloHistories = user.EloHistories;
        var dialects = user.Dialects;
        var jobCompletions = user.JobCompletions;

        var currentElo = stats?.CurrentElo ?? 0;
        var totalJobs = stats?.TotalJobs ?? 0;

        var missingCriteria = this.GetMissingCriteria(currentElo, totalJobs);
        var eligible = missingCriteria.Count == 0;

        var eloTrend7 = this.eloService.GetEloTrend(eloHistories.ToList(), 7);
        var eloTrend30 = this.eloService.GetEloTrend(eloHistories.ToList(), 30);
        var winRate = this.eloService.GetWinRate(eloHistories.ToList());

        var jobsLast30Days = jobCompletions.Count(jc => jc.CompletedAt >= DateTime.UtcNow.AddDays(-30));

        var dto = new UserProfileResponse
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            EloRating = currentElo,
            PeakElo = stats?.PeakElo ?? 0,
            Status = user.Status,
            RegisteredAt = user.CreatedAt,
            IsProfessional = user.Role == UserRoleType.Professional.ToDisplayName(),
            ProfessionalEligibility = new ProfessionalEligibilityDto
            {
                Eligible = eligible,
                MissingCriteria = missingCriteria,
                Progress = new ProfessionalProgressDto
                {
                    EloProgress = $"{currentElo}/{this.minEloRequiredForPro}",
                    JobsProgress = $"{totalJobs}/{this.minJobsRequiredForPro}",
                },
            },
            Statistics = new UserStatisticsDto
            {
                TotalJobsCompleted = totalJobs,
                GamesPlayed = stats?.GamesPlayed ?? 0,
                EloTrend = eloTrend7,
                DialectExpertise = dialects.Select(d => d.Dialect).ToList(),
                WinRate = winRate,
                Last30Days = new Last30DaysStatsDto
                {
                    JobsCompleted = jobsLast30Days,
                    EloChange = eloTrend30,
                    Earnings = 0, // Earning is out of scope for v1.
                },
            },
            Preferences = new UserPreferencesDto
            {
                MaxConcurrentJobs = 3,
                DialectPreferences = user.Dialects.Select(d => d.Dialect).ToList(),
                PreferredJobTypes = new (), // Preferred job type is out of scope for v1.
            },
        };

        return dto;
    }

    /// <inheritdoc/>
    public async Task<User> GetUserAsync(Guid userId)
    {
        var user = await this.repo.GetUserByIdAsync(userId);
        if (user == null)
        {
            this.logger.LogWarning($"User with ID {userId} is not found.");
            throw new KeyNotFoundException("User not found");
        }

        return user;
    }

    /// <inheritdoc/>
    public async Task<User> GetUserByEmailAsync(string email)
    {
        var user = await this.repo.GetUserByEmailAsync(email);
        if (user == null)
        {
            this.logger.LogWarning($"User with ID {email} is not found.");
            throw new KeyNotFoundException("User not found");
        }

        return user;
    }

    /// <inheritdoc/>
    public async Task<List<User>> GetFilteredUser(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit)
    {
        var users = await this.repo.GetFilteredUser(dialect, minElo, maxElo, maxWorkload, limit);
        return users;
    }

    /// <inheritdoc/>
    public async Task UpdateAvailabilityAuditAsync(Guid userId, UserAvailabilityRedisDto existingAvailability, string? ipAddress, string? userAgent)
    {
        await this.auditLogRepo.AddAuditLog(userId, existingAvailability, ipAddress, userAgent);
        await this.auditLogRepo.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<VerificationResponse> ActivateUser(Guid userId, VerificationRequest verificationRequest)
    {
        var requirements = await this.verificationRequirementService.GetEffectiveValidationOptionsAsync(userId);
        if (requirements is null)
        {
            this.logger.LogWarning("Rejecting user verification: Verification requirements not configured.");
            throw new InvalidOperationException("Verification requirements not configured.");
        }

        var user = await this.GetUserAsync(userId);
        if (user is null)
        {
            this.logger.LogWarning($"Rejecting user verification: User not found. User id: {userId}");
            throw new KeyNotFoundException("User not found.");
        }

        // Type check
        if (requirements.RequireIdDocument &&
            verificationRequest.VerificationType != VerificationType.IdDocument.ToDisplayName())
        {
            this.logger.LogWarning($"Rejecting user verification: Verification type must be 'id_document'. Provided value: {verificationRequest.VerificationType}.");
            throw new InvalidOperationException("Verification type must be 'id_document'.");
        }

        // ID Document check
        var idValidation = verificationRequest.IdDocumentValidation;
        if (requirements.RequireIdDocument)
        {
            if (idValidation is null || !idValidation.Enabled)
            {
                this.logger.LogWarning("Rejecting user verification: ID document validation must be enabled.");
                throw new InvalidOperationException("ID document validation must be enabled.");
            }

            var validation = idValidation.ValidationResult;
            if (validation is null ||
                !validation.IdFormatValid ||
                !validation.PhotoPresent ||
                !idValidation.PhotoUploaded)
            {
                this.logger.LogWarning($"Rejecting user verification: ID document validation failed field checks. Provided values: Validation: {validation}, Id format valid : {validation?.IdFormatValid}, Photo present: {validation?.PhotoPresent}, Photo uploaded: {idValidation?.PhotoUploaded}");
                throw new InvalidOperationException("ID document validation failed field checks.");
            }
        }

        // Phone & Email check
        var additional = verificationRequest.AdditionalVerification;

        if (requirements.RequirePhoneVerification && (additional is null || !additional.PhoneVerification))
        {
            this.logger.LogWarning("Rejecting user verification: Phone verification is required.");
            throw new InvalidOperationException("Phone verification is required.");
        }

        if (requirements.RequireEmailVerification && (additional is null || !additional.EmailVerification))
        {
            this.logger.LogWarning("Rejecting user verification: Email verification is required.");
            throw new InvalidOperationException("Email verification is required.");
        }

        // Validate South African ID if provided
        if (!this.ValidateSouthAfricanId(verificationRequest.IdDocumentValidation.IdNumber))
        {
            this.logger.LogWarning($"Registration failed due to invalid South African ID IdNumber: {verificationRequest.IdDocumentValidation.IdNumber}");
            throw new ArgumentException("Invalid South African ID number");
        }

        user.Status = UserStatusType.Active.ToDisplayName();
        user.IdNumber = verificationRequest.IdDocumentValidation.IdNumber;
        var verificationRecord = new VerificationRecord
        {
            VerificationType = verificationRequest.VerificationType,
            Status = VerificationStatusType.Approved.ToDisplayName(),
            VerificationLevel = VerificationLevelType.BasicV1.ToDisplayName(),
            VerificationData = JsonSerializer.Serialize(verificationRequest),
            VerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        user.VerificationRecords.Add(verificationRecord);
        this.repo.Update(user);
        await this.repo.SaveChangesAsync();

        var response = new VerificationResponse
        {
            VerificationStatus = verificationRecord.Status,
            EloRating = user.Statistics?.CurrentElo ?? 0,
            StatusChanged = "pending_verification -> active",
            EligibleForWork = true,
            ActivationMethod = "automatic",
            ActivatedAt = DateTime.UtcNow,
            VerificationLevel = verificationRecord.VerificationLevel,
            NextSteps = new string[] { "profile_completion", "job_browsing" },
            RoadmapNote = "V2 will include automated ID verification via Department of Home Affairs",
        };
        return response;
    }

    /// <inheritdoc/>
    public async Task<bool> CheckIdNumber(Guid userId, string idNumber)
    {
        var user = await this.repo.GetUserByIdAsync(userId);
        if (user == null)
        {
            this.logger.LogWarning($"Id number get failed because user with ID {userId} is not found.");
            return false;
        }

        return user.IdNumber == idNumber;
    }

    /// <inheritdoc/>
    public async Task<ClaimJobResponse> ClaimJobAsync(Guid userId, ClaimJobRequest claimJobRequest, List<Guid>? originalTranscribers = null, int? requiredMinElo = null)
    {
        var claimId = Guid.NewGuid();
        var availabilityCache = await this.redisService.GetAvailabilityAsync(userId);
        var userEloCache = await this.redisService.GetUserEloAsync(userId);
        var userDb = new User();
        var userjobClaims = await this.redisService.GetUserClaimsAsync(userId);
        if (userjobClaims.Contains(claimJobRequest.JobId))
        {
            this.logger.LogWarning($"User: {userId} already claimed the job: {claimJobRequest.JobId}");
            throw new InvalidOperationException("User already claimed the job:");
        }

        if (userEloCache == null)
        {
            userDb = await this.repo.GetUserByIdAsync(userId, false, false, u => u.Statistics!, u => u.EloHistories, u => u.JobCompletions);
            if (userDb == null)
            {
                this.logger.LogWarning($"User with ID {userId} is not found.");
                throw new KeyNotFoundException("User not found");
            }
        }

        // Elo cache miss
        if (userEloCache == null)
        {
            if (userDb != null && userDb.Statistics != null)
            {
                var userStats = userDb.Statistics;
                var elohistories = userDb.EloHistories.ToList();
                var lastJobCompleted = userDb.JobCompletions.MaxBy(jc => jc.CompletedAt)?.CompletedAt;
                userEloCache = new UserEloRedisDto
                {
                    CurrentElo = userStats.CurrentElo,
                    PeakElo = userStats.PeakElo,
                    GamesPlayed = userStats.GamesPlayed,
                    RecentTrend = this.eloService.GetEloTrend(elohistories, 7),
                    LastJobCompleted = lastJobCompleted ?? default,
                };

                await this.redisService.SetUserEloAsync(userId, userEloCache);
            }
        }

        if (availabilityCache == null || availabilityCache.Status != UserAvailabilityType.Available.ToDisplayName())
        {
            this.logger.LogWarning($"Rejecting tiebreaker claim: User is currently unavailable for work. User Id: {userId}");
            throw new ArgumentException("Rejecting tiebreaker claim: User is currently unavailable for work.");
        }

        if (availabilityCache.CurrentWorkload >= availabilityCache.MaxConcurrentJobs)
        {
            this.logger.LogWarning($"Rejecting tiebreaker claim: User already has maximum concurrent jobs. CurrentWorkload: {availabilityCache.CurrentWorkload}, MaxConcurrentJobs: {availabilityCache.MaxConcurrentJobs}");
            throw new ArgumentException("Rejecting tiebreaker claim: User already has maximum concurrent jobs.");
        }

        if (originalTranscribers != null && originalTranscribers.Contains(userId))
        {
            this.logger.LogWarning($"Rejecting tiebreaker claim: User is original transcriber.");
            throw new ArgumentException("Rejecting tiebreaker claim: User is original transcriber.");
        }

        if (requiredMinElo != null && requiredMinElo > userEloCache!.CurrentElo)
        {
            this.logger.LogWarning($"Rejecting tiebreaker claim: User already has maximum concurrent jobs. CurrentWorkload: {availabilityCache.CurrentWorkload}, MaxConcurrentJobs: {availabilityCache.MaxConcurrentJobs}");
            throw new ArgumentException("Rejecting tiebreaker claim: User already has maximum concurrent jobs.");
        }

        var tryLockJobClaim = await this.redisService.TryClaimJobAsync(claimJobRequest.JobId, userId);
        if (!tryLockJobClaim)
        {
            this.logger.LogWarning($"Rejecting tiebreaker claim:Job is already claimed by another user. Job id: {claimJobRequest.JobId}");
            throw new ArgumentException("Rejecting tiebreaker claim:Job is already claimed by another user.");
        }

        this.logger.LogInformation($"Job: {claimJobRequest.JobId} successfully locked by user: {userId}.");

        availabilityCache.CurrentWorkload++;
        await this.redisService.AddUserClaimAsync(userId, claimJobRequest.JobId);
        await this.redisService.SetAvailabilityAsync(userId, availabilityCache);
        var jobClaim = new JobClaim
        {
            Id = claimId,
            UserId = userId,
            JobId = claimJobRequest.JobId,
            ClaimedAt = claimJobRequest.ClaimTimestamp,
            BookOutExpiresAt = DateTime.UtcNow.AddMinutes(this.defaultBookoutInMinutes),
            Status = JobClaimStatus.Pending.ToDisplayName(),
            CreatedAt = DateTime.UtcNow,
        };
        jobClaim = await this.jobClaimRepo.AddJobClaimAsync(jobClaim);
        await this.jobClaimRepo.SaveChangesAsync();

        await this.redisService.ReleaseJobClaimAsync(claimJobRequest.JobId);
        this.logger.LogInformation($"Job: {claimJobRequest.JobId} lock successfully released by user: {userId}.");

        var response = new ClaimJobResponse
        {
            ClaimValidated = true,
            UserEligible = true,
            ClaimId = claimId,
            UserAvailability = availabilityCache.Status,
            CurrentWorkload = availabilityCache.CurrentWorkload,
            MaxConcurrentJobs = availabilityCache.MaxConcurrentJobs,
            CapacityReservedUntil = DateTime.UtcNow.AddMinutes(this.defaultBookoutInMinutes),
            CurrentElo = userEloCache!.CurrentElo,
        };
        return response;
    }

    /// <inheritdoc/>
    public async Task<ValidateTiebreakerClaimResponse> ValidateTieBreakerClaim(Guid userId, ValidateTiebreakerClaimRequest validationReq)
    {
        var claimId = Guid.NewGuid();
        var claimJobRequest = new ClaimJobRequest
        {
            JobId = validationReq.OriginalJobId,
            ClaimTimestamp = DateTime.UtcNow,
        };

        var jobClaim = await this.ClaimJobAsync(userId, claimJobRequest, validationReq.OriginalTranscribers, validationReq.RequiredMinElo);
        this.logger.LogInformation($"Job: {claimJobRequest.JobId} successfully claimed by user: {userId}. Claim Id: {claimId}");

        return new ValidateTiebreakerClaimResponse
        {
            TiebreakerClaimValidated = true,
            UserId = userId,
            UserEloQualified = true,
            CurrentElo = jobClaim.CurrentElo,
            IsOriginalTranscriber = validationReq.OriginalTranscribers.Contains(userId),
            ClaimId = claimId.ToString(),
            BonusConfirmed = true,
            EstimatedCompletion = string.Empty, // Calculation not covered in v1
        };
    }

    /// <inheritdoc/>
    public async Task<SetProfessionalResponse> SetProfessional(Guid userId, SetProfessionalRequest validationReq)
    {
        var user = await this.repo.GetUserByIdAsync(userId, true, false, u => u.Statistics!, userId => userId.VerificationRecords);
        var authorizedBy = await this.repo.GetUserByIdAsync(validationReq.AuthorizedBy);
        if (user == null || user.Statistics == null)
        {
            this.logger.LogWarning($"Setting professional status failed because User with ID {userId} not found.");
            throw new KeyNotFoundException("User not found");
        }

        if (authorizedBy == null)
        {
            this.logger.LogWarning($"Setting professional status failed because Autorizer with ID {validationReq.AuthorizedBy} not found.");
            throw new KeyNotFoundException("Authorizer not found");
        }

        if (authorizedBy.Role != UserRoleType.Admin.ToDisplayName())
        {
            this.logger.LogWarning($"Setting professional failed because authorizer is not admin.");
            throw new ArgumentException("Authorizer must be an admin");
        }

        var missingCriteria = this.GetMissingCriteria(user.Statistics.CurrentElo, user.Statistics.TotalJobs);
        var eligible = missingCriteria.Count == 0;
        if (!eligible)
        {
            throw new ArgumentException("Minimum criteria not met.");
        }

        var previousRole = user.Role;

        user.IsProfessional = validationReq.IsProfessional;

        user.VerificationRecords.Add(new VerificationRecord
        {
            VerificationData = JsonSerializer.Serialize(validationReq.ProfessionalVerification.VerificationDocuments),
            VerificationType = VerificationType.IdDocument.ToDisplayName(),
            Status = VerificationStatusType.Approved.ToDisplayName(),
            VerificationLevel = VerificationLevelType.BasicV1.ToDisplayName(),
            VerifiedBy = authorizedBy.Id,
            VerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        user.Role = validationReq.IsProfessional ? UserRoleType.Professional.ToDisplayName() : UserRoleType.Transcriber.ToDisplayName();

        await this.repo.SaveChangesAsync();

        return new SetProfessionalResponse
        {
            UserId = user.Id,
            RoleUpdated = true,
            IsProfessional = user.IsProfessional,
            PreviousRole = previousRole,
            NewRole = user.Role,
            EffectiveFrom = DateTime.UtcNow,
        };
    }

    /// <inheritdoc/>
    public async Task<GetProfessionalStatusResponse> GetProfessionalStatus(Guid userId)
    {
        var user = await this.repo.GetUserByIdAsync(userId, false, false, u => u.Statistics!);
        if (user == null || user.Statistics == null)
        {
            this.logger.LogWarning($"Getting professional status failed because User with ID {userId} not found.");
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        var verificationRecord = this.GetLastProfessionalVerificationRecord(user);

        var response = new GetProfessionalStatusResponse
        {
            UserId = user.Id,
            CurrentElo = user.Statistics!.CurrentElo,
        };

        if (user.IsProfessional && verificationRecord != null)
        {
            response.IsProfessional = true;
            response.ProfessionalDetails = new ProfessionalDetailsDto
            {
                Designation = user.Role,
                Level = this.GetSkillLevelFromElo(user.Statistics.CurrentElo),
                BypassQAComparison = user.IsProfessional,
                DesignatedAt = verificationRecord.VerifiedAt!.Value,
                DesignatedBy = verificationRecord.VerifiedBy!.Value.ToString(),
            };
            response.TotalJobsCompleted = user.Statistics.TotalJobs;
        }
        else
        {
            response.IsProfessional = false;
            response.CurrentRole = user.Role;
            response.EligibleForProfessional = user.Statistics.CurrentElo >= this.minEloRequiredForPro && user.Statistics.TotalJobs >= this.minJobsRequiredForPro;
            response.EligibilityCriteria = new EligibilityCriteriaDto
            {
                MinEloRequired = this.minEloRequiredForPro,
                MinJobsRequired = this.minJobsRequiredForPro,
                UserElo = user.Statistics.CurrentElo,
                UserJobs = user.Statistics.TotalJobs,
            };
        }

        return response;
    }

    /// <inheritdoc/>
    public async Task<ProfessionalStatusBatchResponse> GetBatchProfessionalStatus(List<Guid> userIds)
    {
        var users = await this.repo.GetFilteredListAsync(u => userIds.Contains(u.Id));
        if (users == null || users.Count == 0)
        {
            this.logger.LogWarning($"Getting batch professional status failed because Users with ID {userIds} not found.");
            throw new KeyNotFoundException($"No user not found.");
        }

        var response = new ProfessionalStatusBatchResponse();

        foreach (var user in users)
        {
            response.ProfessionalStatuses[user.Id.ToString()] = new ProfessionalStatus
            {
                IsProfessional = user.IsProfessional,
                BypassQAComparison = user.IsProfessional,
            };
        }

        response.Summary = new ProfessionalSummary
        {
            TotalChecked = users.Count,
            Professionals = users.Count(u => u.IsProfessional),
            StandardTranscribers = users.Count(u => !u.IsProfessional),
        };
        return response;
    }

    /// <inheritdoc/>
    public async Task<UserLoginResponse?> AuthenticateAsync(UserLoginRequest request)
    {
        var user = await this.repo.GetUserByEmailAsync(request.Username);

        if (user == null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            this.logger.LogWarning($"Authentication failed because of invalid credentials.");
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(this.jwtOptions.Value.Secret);

        var claims = new List<Claim>
        {
            new (ClaimTypes.NameIdentifier, user!.Id.ToString()),
            new (ClaimTypes.Email, user.Email),
            new (ClaimTypes.Role, user.Role.ToString()),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(this.jwtOptions.Value.ExpiryMinutes),
            Issuer = this.jwtOptions.Value.Issuer,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new UserLoginResponse
        {
            AccessToken = tokenHandler.WriteToken(token),
            ExpiresAt = tokenDescriptor.Expires!.Value,
        };
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var userId = Guid.Parse(this.httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) !);
        var user = await this.repo.GetUserByIdAsync(userId, true);
        if (user == null)
        {
            this.logger.LogError("Invalid user Id.");
            throw new ArgumentException("User not found");
        }

        if (!PasswordHasher.Verify(currentPassword, user.PasswordHash))
        {
            this.logger.LogWarning($"Password change failed: incorrect current password for user {user.Email}");
            return (false, "Current password is incorrect");
        }

        var passwordValidationError = this.ValidatePassword(newPassword);
        if (passwordValidationError != null)
        {
            this.logger.LogWarning($"Password change failed for user {user.Email}: {passwordValidationError}");
            return (false, passwordValidationError);
        }

        user.PasswordHash = PasswordHasher.Hash(newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await this.repo.SaveChangesAsync();
        this.logger.LogInformation($"Password changed successfully for user {user.Email}");
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<UserAvailabilityResponse> GetUserAvailabilitySummaryAsync(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit)
    {
        var availableUsersResp = new UserAvailabilityResponse();
        var users = await this.GetFilteredUser(dialect, minElo, maxElo, maxWorkload, limit);
        if (!users.Any())
        {
            this.logger.LogWarning("No user found!");
            return availableUsersResp;
        }

        var cacheMap = await this.redisService.GetBulkAvailabilityAsync(users.Select(u => u.Id));
        var trendMap = await this.eloService.BulkEloTrendAsync(users.Select(u => u.Id).ToList(), 7);
        availableUsersResp.AvailableUsers = users
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
        availableUsersResp.TotalAvailable = availableUsersResp.AvailableUsers.Count;
        availableUsersResp.QueryTimestamp = DateTime.UtcNow;
        return availableUsersResp;
    }

    /// <inheritdoc/>
    public async Task<UserAvailabilityRedisDto?> GetAvailabilityAsync(Guid userId)
    {
        return await this.redisService.GetAvailabilityAsync(userId);
    }

    /// <inheritdoc/>
    public async Task<UserAvailabilityUpdateResponse> PatchAvailabilityAsync(Guid userId, UserAvailabilityUpdateRequest availabilityUpdateRequest)
    {
        var ipAddress = this.httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = this.httpContextAccessor?.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";
        var existingAvailability = await this.redisService.GetAvailabilityAsync(userId)
                            ?? new UserAvailabilityRedisDto();

        if (!EnumDisplayHelper.TryParseDisplayName(availabilityUpdateRequest.Status, out UserAvailabilityType outcome))
        {
            this.logger.LogWarning($"Rejecting availability update: Invalid Status. Provided value: {availabilityUpdateRequest.Status}");
            throw new ArgumentException("Invalid Status Provided.");
        }

        if (availabilityUpdateRequest.MaxConcurrentJobs < 1)
        {
            this.logger.LogWarning("Rejecting availability update: MaxConcurrentJobs must be greater than 0. Provided value: {Value}", availabilityUpdateRequest.MaxConcurrentJobs);
            throw new ArgumentException("Maximum concurrent job should be greater than 0");
        }

        if (availabilityUpdateRequest != null)
        {
            existingAvailability.Status = availabilityUpdateRequest.Status;
            existingAvailability.MaxConcurrentJobs = availabilityUpdateRequest.MaxConcurrentJobs;
        }

        existingAvailability.LastUpdate = DateTime.UtcNow;

        // Asynchrounously write to redis and persis in DB.
        var redisTask = this.redisService.SetAvailabilityAsync(userId, existingAvailability);
        var auditTask = this.UpdateAvailabilityAuditAsync(userId, existingAvailability, ipAddress, userAgent);

        await Task.WhenAll(redisTask, auditTask);

        return new UserAvailabilityUpdateResponse
        {
            AvailabilityUpdated = "success",
            CurrentStatus = existingAvailability.Status,
            MaxConcurrentJobs = existingAvailability.MaxConcurrentJobs,
            LastUpdated = existingAvailability.LastUpdate,
        };
    }

    // Private methods.
    private List<string> GetMissingCriteria(int elo, int totalJobs)
    {
        List<string> missingCriteria = new List<string>();
        if (elo < this.minEloRequiredForPro)
        {
            missingCriteria.Add("elo_rating");
        }

        if (totalJobs < this.minJobsRequiredForPro)
        {
            missingCriteria.Add("total_jobs");
        }

        return missingCriteria;
    }

    private string GetSkillLevelFromElo(int elo)
    {
        if (elo >= 1600)
        {
            return "expert";
        }

        if (elo >= 1400)
        {
            return "advanced";
        }

        if (elo >= 1200)
        {
            return "intermediate";
        }

        return "novice";
    }

    private VerificationRecord? GetLastProfessionalVerificationRecord(User user)
    {
        return user.VerificationRecords
            .Where(v =>
                v.VerificationType == VerificationType.IdDocument.ToDisplayName() &&
                v.Status == VerificationStatusType.Approved.ToDisplayName() &&
                user.IsProfessional &&
                user.Role == UserRoleType.Professional.ToDisplayName())
            .OrderByDescending(v => v.VerifiedAt)
            .FirstOrDefault();
    }

    private string? ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required";
        }

        if (password.Length < 8)
        {
            return "Password must be at least 8 characters long";
        }

        if (!password.Any(char.IsUpper))
        {
            return "Password must contain at least one uppercase letter";
        }

        if (!password.Any(char.IsLower))
        {
            return "Password must contain at least one lowercase letter";
        }

        if (!password.Any(char.IsDigit))
        {
            return "Password must contain at least one digit";
        }

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            return "Password must contain at least one special character";
        }

        return null; // Valid password
    }

    private bool ValidateSouthAfricanId(string? idNumber)
    {
        if (string.IsNullOrEmpty(idNumber))
        {
            return true; // Accept null or empty
        }

        if (idNumber.Length != 13)
        {
            return false;
        }

        if (!idNumber.All(char.IsDigit))
        {
            return false;
        }

        return true; // Simple length and digit check only
    }
}
// <copyright file="UserService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CloudNative.CloudEvents;
using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Abstractions.Services;
using CohesionX.UserManagement.Application.Constants;
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
using SharedLibrary.Kafka.Services.Interfaces;

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
    private readonly IEventBus eventBus;
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
    /// <param name="eventBus">Service for publishing events.</param>
    /// <param name="httpContextAccessor"> Http Context accessor.</param>
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
        IEventBus eventBus,
        IHttpContextAccessor httpContextAccessor,
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
        this.eventBus = eventBus;
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
            this.logger.LogWarning("Rejecting registration: Consent to data processing needed! Email: {Email}", dto.Email);
            throw new ArgumentException("Consent to data processing needed!");
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.FirstName) ||
            string.IsNullOrWhiteSpace(dto.LastName) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            this.logger.LogWarning("Registration failed due to missing fields. Payload: {@Payload}", dto);
            throw new ArgumentException("All required fields must be provided");
        }

        // Validate password strength
        var passwordError = this.ValidatePassword(dto.Password);
        if (passwordError != null)
        {
            this.logger.LogWarning("Registration failed due to weak password. Email: {Email}, Reason: {Reason}", dto.Email, passwordError);
            throw new ArgumentException(passwordError);
        }

        // Validate South African ID if provided
        if (!this.ValidateSouthAfricanId(dto.IdNumber))
        {
            this.logger.LogWarning("Registration failed due to invalid South African ID. Email: {Email}, IdNumber: {IdNumber}", dto.Email, dto.IdNumber);
            throw new ArgumentException("Invalid South African ID number");
        }

        // Check if email already exists
        if (await this.repo.EmailExistsAsync(dto.Email))
        {
            this.logger.LogWarning("Registration failed: Email already exists. Email: {Email}", dto.Email);
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

            this.logger.LogDebug("Dialect preferences added for user {UserId}: {@Dialects}", user.Id, dto.DialectPreferences);
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

        var verificationRequired = new List<string> { "id_document_upload" };

        await this.repo.AddAsync(user);
        await this.repo.SaveChangesAsync();

        this.logger.LogInformation("User registered successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);

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
            this.logger.LogWarning("Profile retrieval failed: User not found. UserId: {UserId}", userId);
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

        this.logger.LogInformation("User profile retrieved. UserId: {UserId}, CurrentElo: {CurrentElo}, TotalJobs: {TotalJobs}, EligibleForPro: {Eligible}", userId, currentElo, totalJobs, eligible);

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
                    Earnings = 0,
                },
            },
            Preferences = new UserPreferencesDto
            {
                MaxConcurrentJobs = 3,
                DialectPreferences = user.Dialects.Select(d => d.Dialect).ToList(),
                PreferredJobTypes = new (),
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
            this.logger.LogWarning("User retrieval failed: User with ID {UserId} not found.", userId);
            throw new KeyNotFoundException("User not found");
        }

        this.logger.LogInformation("User retrieved successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);
        return user;
    }

    /// <inheritdoc/>
    public async Task<User> GetUserByEmailAsync(string email)
    {
        var user = await this.repo.GetUserByEmailAsync(email);
        if (user == null)
        {
            this.logger.LogWarning("User retrieval failed: User with Email {Email} not found.", email);
            throw new KeyNotFoundException("User not found");
        }

        this.logger.LogInformation("User retrieved successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);
        return user;
    }

    /// <inheritdoc/>
    public async Task<List<User>> GetFilteredUser(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit)
    {
        var users = await this.repo.GetFilteredUser(dialect, minElo, maxElo, maxWorkload, limit);
        this.logger.LogInformation("Filtered users retrieved. Count: {Count}, Filter: {{Dialect: {Dialect}, MinElo: {MinElo}, MaxElo: {MaxElo}, MaxWorkload: {MaxWorkload}, Limit: {Limit}}}", users.Count, dialect, minElo, maxElo, maxWorkload, limit);

        return users;
    }

    /// <inheritdoc/>
    public async Task UpdateAvailabilityAuditAsync(Guid userId, UserAvailabilityRedisDto existingAvailability, string? ipAddress, string? userAgent)
    {
        this.logger.LogInformation("Updating availability audit for user {UserId}. IP: {IpAddress}, UserAgent: {UserAgent}", userId, ipAddress, userAgent);
        await this.auditLogRepo.AddAuditLog(userId, existingAvailability, ipAddress, userAgent);
        await this.auditLogRepo.SaveChangesAsync();
        this.logger.LogInformation("Availability audit updated successfully for user {UserId}.", userId);
    }

    /// <inheritdoc/>
    public async Task<VerificationResponse> ActivateUser(Guid userId, VerificationRequest verificationRequest)
    {
        var requirements = await this.verificationRequirementService.GetEffectiveValidationOptionsAsync(userId);
        if (requirements is null)
        {
            this.logger.LogWarning("Rejecting user verification: Verification requirements not configured for user {UserId}.", userId);
            throw new InvalidOperationException("Verification requirements not configured.");
        }

        var user = await this.GetUserAsync(userId);
        this.logger.LogInformation("Activating user {UserId}. Current status: {Status}", userId, user.Status);

        // Type check
        if (requirements.RequireIdDocument &&
            verificationRequest.VerificationType != VerificationType.IdDocument.ToDisplayName())
        {
            this.logger.LogWarning("Rejecting user verification for user {UserId}: Verification type must be 'id_document'. Provided: {Type}", userId, verificationRequest.VerificationType);
            throw new InvalidOperationException("Verification type must be 'id_document'.");
        }

        // ID Document check
        var idValidation = verificationRequest.IdDocumentValidation;
        if (requirements.RequireIdDocument)
        {
            if (idValidation is null || !idValidation.Enabled)
            {
                this.logger.LogWarning("Rejecting user verification for user {UserId}: ID document validation not enabled.", userId);
                throw new InvalidOperationException("ID document validation must be enabled.");
            }

            var validation = idValidation.ValidationResult;
            if (validation is null ||
                !validation.IdFormatValid ||
                !validation.PhotoPresent ||
                !idValidation.PhotoUploaded)
            {
                this.logger.LogWarning(
                    "Rejecting user verification for user {UserId}: ID document validation failed. IdFormatValid: {IdFormatValid}, PhotoPresent: {PhotoPresent}, PhotoUploaded: {PhotoUploaded}", userId, validation?.IdFormatValid, validation?.PhotoPresent, idValidation?.PhotoUploaded);
                throw new InvalidOperationException("ID document validation failed field checks.");
            }
        }

        // Phone & Email check
        var additional = verificationRequest.AdditionalVerification;
        if (requirements.RequirePhoneVerification && (additional is null || !additional.PhoneVerification))
        {
            this.logger.LogWarning("Rejecting user verification for user {UserId}: Phone verification required.", userId);
            throw new InvalidOperationException("Phone verification is required.");
        }

        if (requirements.RequireEmailVerification && (additional is null || !additional.EmailVerification))
        {
            this.logger.LogWarning("Rejecting user verification for user {UserId}: Email verification required.", userId);
            throw new InvalidOperationException("Email verification is required.");
        }

        // Validate South African ID if provided
        if (!this.ValidateSouthAfricanId(verificationRequest.IdDocumentValidation.IdNumber))
        {
            this.logger.LogWarning("Rejecting user verification for user {UserId}: Invalid South African ID {IdNumber}", userId, verificationRequest.IdDocumentValidation.IdNumber);
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

        this.logger.LogInformation("User {UserId} activated successfully. Verification status: {Status}", userId, verificationRecord.Status);

        return new VerificationResponse
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
    }

    /// <inheritdoc/>
    public async Task<bool> CheckIdNumber(Guid userId, string idNumber)
    {
        var user = await this.repo.GetUserByIdAsync(userId);
        if (user == null)
        {
            this.logger.LogWarning("ID number check failed: User with ID {UserId} not found.", userId);
            return false;
        }

        this.logger.LogInformation("ID number check for user {UserId}: Provided {Provided}, Actual {Actual}", userId, idNumber, user.IdNumber);
        return user.IdNumber == idNumber;
    }

    /// <inheritdoc/>
    public async Task<ClaimJobResponse> ClaimJobAsync(Guid userId, ClaimJobRequest claimJobRequest, List<Guid>? originalTranscribers = null, int? requiredMinElo = null)
    {
        var claimId = Guid.NewGuid();
        this.logger.LogInformation("User {UserId} attempting to claim job {JobId}.", userId, claimJobRequest.JobId);

        var availabilityCache = await this.redisService.GetAvailabilityAsync(userId);
        var userEloCache = await this.redisService.GetUserEloAsync(userId);
        var userDb = new User();
        var userjobClaims = await this.redisService.GetUserClaimsAsync(userId);

        if (userjobClaims.Contains(claimJobRequest.JobId))
        {
            this.logger.LogWarning("User {UserId} already claimed job {JobId}.", userId, claimJobRequest.JobId);
            throw new InvalidOperationException("User already claimed the job:");
        }

        if (userEloCache == null)
        {
            userDb = await this.repo.GetUserByIdAsync(userId, false, false, u => u.Statistics!, u => u.EloHistories, u => u.JobCompletions);
            if (userDb == null)
            {
                this.logger.LogWarning("User {UserId} not found during Elo cache miss.", userId);
                throw new KeyNotFoundException("User not found");
            }
        }

        // Elo cache miss logic
        if (userEloCache == null && userDb?.Statistics != null)
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
            this.logger.LogInformation("Elo cache set for user {UserId}. CurrentElo: {Elo}", userId, userEloCache.CurrentElo);
        }

        if (availabilityCache == null || availabilityCache.Status != UserAvailabilityType.Available.ToDisplayName())
        {
            this.logger.LogWarning("User {UserId} unavailable to claim job {JobId}. Status: {Status}", userId, claimJobRequest.JobId, availabilityCache?.Status);
            throw new ArgumentException("Rejecting tiebreaker claim: User is currently unavailable for work.");
        }

        if (availabilityCache.CurrentWorkload >= availabilityCache.MaxConcurrentJobs)
        {
            this.logger.LogWarning("User {UserId} reached max concurrent jobs: {Workload}/{Max}", userId, availabilityCache.CurrentWorkload, availabilityCache.MaxConcurrentJobs);
            throw new ArgumentException("Rejecting tiebreaker claim: User already has maximum concurrent jobs.");
        }

        if (originalTranscribers != null && originalTranscribers.Contains(userId))
        {
            this.logger.LogWarning("User {UserId} is original transcriber for job {JobId}, cannot claim.", userId, claimJobRequest.JobId);
            throw new ArgumentException("Rejecting tiebreaker claim: User is original transcriber.");
        }

        if (requiredMinElo != null && requiredMinElo > userEloCache!.CurrentElo)
        {
            this.logger.LogWarning("User {UserId} does not meet minimum Elo {RequiredElo}, current {CurrentElo}", userId, requiredMinElo, userEloCache.CurrentElo);
            throw new ArgumentException("Rejecting tiebreaker claim: User Elo too low.");
        }

        var tryLockJobClaim = await this.redisService.TryClaimJobAsync(claimJobRequest.JobId, userId);
        if (!tryLockJobClaim)
        {
            this.logger.LogWarning("Job {JobId} already claimed by another user, user {UserId} cannot claim.", claimJobRequest.JobId, userId);
            throw new ArgumentException("Rejecting tiebreaker claim:Job is already claimed by another user.");
        }

        this.logger.LogInformation("Job {JobId} successfully locked by user {UserId}.", claimJobRequest.JobId, userId);

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
        this.logger.LogInformation("Job {JobId} lock successfully released by user {UserId}.", claimJobRequest.JobId, userId);

        var cloudEvent = new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri($"{TopicConstant.UserJobClaimed}:{userId}"),
            Type = TopicConstant.UserJobClaimed,
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json",
            Data = new { UserId = userId, Message = "Users Claimed job.", Data = claimJobRequest },
        };
        try
        {
            await this.eventBus.PublishAsync(cloudEvent, TopicConstant.UserJobClaimed);
            this.logger.LogInformation("User {UserId} job claim CloudEvent published successfully.", userId);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "User {UserId} job claim CloudEvent publish failed.", userId);
        }

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

        this.logger.LogInformation("User {UserId} successfully claimed job {JobId}.", userId, claimJobRequest.JobId);
        return response;
    }

    /// <inheritdoc/>
    public async Task<ValidateTiebreakerClaimResponse> ValidateTieBreakerClaim(Guid userId, ValidateTiebreakerClaimRequest validationReq)
    {
        this.logger.LogInformation("Validating tiebreaker claim for user {UserId} on job {JobId}.", userId, validationReq.OriginalJobId);

        var claimId = Guid.NewGuid();
        var claimJobRequest = new ClaimJobRequest
        {
            JobId = validationReq.OriginalJobId,
            ClaimTimestamp = DateTime.UtcNow,
        };

        var jobClaim = await this.ClaimJobAsync(userId, claimJobRequest, validationReq.OriginalTranscribers, validationReq.RequiredMinElo);
        this.logger.LogInformation("Tiebreaker claim successful for user {UserId} on job {JobId}, ClaimId: {ClaimId}", userId, claimJobRequest.JobId, claimId);

        return new ValidateTiebreakerClaimResponse
        {
            TiebreakerClaimValidated = true,
            UserId = userId,
            UserEloQualified = true,
            CurrentElo = jobClaim.CurrentElo,
            IsOriginalTranscriber = validationReq.OriginalTranscribers.Contains(userId),
            ClaimId = claimId.ToString(),
            BonusConfirmed = true,
            EstimatedCompletion = string.Empty,
        };
    }

    /// <inheritdoc/>
    public async Task<SetProfessionalResponse> SetProfessional(Guid userId, SetProfessionalRequest validationReq)
    {
        this.logger.LogInformation("Setting professional status for user {UserId} requested by {Authorizer}.", userId, validationReq.AuthorizedBy);

        var user = await this.repo.GetUserByIdAsync(userId, true, false, u => u.Statistics!, userId => userId.VerificationRecords);
        var authorizedBy = await this.repo.GetUserByIdAsync(validationReq.AuthorizedBy);

        if (user == null || user.Statistics == null)
        {
            this.logger.LogWarning("Cannot set professional status: User {UserId} not found.", userId);
            throw new KeyNotFoundException("User not found");
        }

        if (authorizedBy == null)
        {
            this.logger.LogWarning("Cannot set professional status: Authorizer {AuthorizerId} not found.", validationReq.AuthorizedBy);
            throw new KeyNotFoundException("Authorizer not found");
        }

        if (authorizedBy.Role != UserRoleType.Admin.ToDisplayName())
        {
            this.logger.LogWarning("Cannot set professional status: Authorizer {AuthorizerId} is not admin.", validationReq.AuthorizedBy);
            throw new ArgumentException("Authorizer must be an admin");
        }

        var missingCriteria = this.GetMissingCriteria(user.Statistics.CurrentElo, user.Statistics.TotalJobs);
        var eligible = missingCriteria.Count == 0;
        if (!eligible)
        {
            this.logger.LogWarning("Cannot set professional status for user {UserId}: Minimum criteria not met.", userId);
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

        this.logger.LogInformation("Professional status set for user {UserId}. PreviousRole: {PreviousRole}, NewRole: {NewRole}", userId, previousRole, user.Role);

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
        this.logger.LogInformation("Fetching professional status for user {UserId}.", userId);

        var user = await this.repo.GetUserByIdAsync(userId, false, false, u => u.Statistics!);
        if (user == null || user.Statistics == null)
        {
            this.logger.LogWarning("Professional status fetch failed: User {UserId} not found.", userId);
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        var verificationRecord = this.GetLastProfessionalVerificationRecord(user);

        var response = new GetProfessionalStatusResponse
        {
            UserId = user.Id,
            CurrentElo = user.Statistics.CurrentElo,
        };

        if (user.IsProfessional && verificationRecord != null)
        {
            this.logger.LogInformation("User {UserId} is professional.", userId);
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
            this.logger.LogInformation("User {UserId} is not professional.", userId);
            response.IsProfessional = false;
            response.CurrentRole = user.Role;
            response.EligibleForProfessional = user.Statistics.CurrentElo >= this.minEloRequiredForPro &&
                                               user.Statistics.TotalJobs >= this.minJobsRequiredForPro;
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
        this.logger.LogInformation("Fetching batch professional status for {Count} users.", userIds.Count);

        var users = await this.repo.GetFilteredListAsync(u => userIds.Contains(u.Id));
        if (users == null || users.Count == 0)
        {
            this.logger.LogWarning("Batch professional status fetch failed: Users {UserIds} not found.", string.Join(", ", userIds));
            throw new KeyNotFoundException("No user found.");
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

        this.logger.LogInformation("Batch professional status fetched successfully. TotalChecked: {TotalChecked}, Professionals: {Professionals}, StandardTranscribers: {StandardTranscribers}", response.Summary.TotalChecked, response.Summary.Professionals, response.Summary.StandardTranscribers);

        return response;
    }

    /// <inheritdoc/>
    public async Task<UserLoginResponse?> AuthenticateAsync(UserLoginRequest request)
    {
        this.logger.LogInformation("Authenticating user with username/email {Username}.", request.Username);

        var user = await this.repo.GetUserByEmailAsync(request.Username);

        if (user == null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            this.logger.LogWarning("Authentication failed for username/email {Username}. Invalid credentials.", request.Username);
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
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        this.logger.LogInformation("User {UserId} authenticated successfully.", user.Id);

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
        this.logger.LogInformation("User {UserId} is attempting to change password.", userId);

        var user = await this.repo.GetUserByIdAsync(userId, true);
        if (user == null)
        {
            this.logger.LogError("Password change failed: User {UserId} not found.", userId);
            throw new ArgumentException("User not found");
        }

        if (!PasswordHasher.Verify(currentPassword, user.PasswordHash))
        {
            this.logger.LogWarning("Password change failed: incorrect current password for user {Email}.", user.Email);
            return (false, "Current password is incorrect");
        }

        var passwordValidationError = this.ValidatePassword(newPassword);
        if (passwordValidationError != null)
        {
            this.logger.LogWarning("Password change failed for user {Email}: {Reason}", user.Email, passwordValidationError);
            return (false, passwordValidationError);
        }

        user.PasswordHash = PasswordHasher.Hash(newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await this.repo.SaveChangesAsync();
        this.logger.LogInformation("Password changed successfully for user {Email}.", user.Email);
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<UserAvailabilityResponse> GetUserAvailabilitySummaryAsync(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit)
    {
        this.logger.LogInformation("Fetching user availability summary. Filters - Dialect: {Dialect}, MinElo: {MinElo}, MaxElo: {MaxElo}, MaxWorkload: {MaxWorkload}, Limit: {Limit}", dialect, minElo, maxElo, maxWorkload, limit);

        var availableUsersResp = new UserAvailabilityResponse();
        var users = await this.GetFilteredUser(dialect, minElo, maxElo, maxWorkload, limit);

        if (!users.Any())
        {
            this.logger.LogWarning("No users found matching the provided filters.");
            return availableUsersResp;
        }

        var cacheMap = await this.redisService.GetBulkAvailabilityAsync(users.Select(u => u.Id));
        var trendMap = await this.eloService.BulkEloTrendAsync(users.Select(u => u.Id).ToList(), 7);

        availableUsersResp.AvailableUsers = users
            .Where(u => cacheMap.ContainsKey(u.Id) && cacheMap[u.Id].Status == UserAvailabilityType.Available.ToDisplayName())
            .Select(u =>
            {
                var availability = cacheMap.ContainsKey(u.Id) ? cacheMap[u.Id] : null;

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

        this.logger.LogInformation("User availability summary fetched. Total available users: {Count}", availableUsersResp.TotalAvailable);
        return availableUsersResp;
    }

    /// <inheritdoc/>
    public async Task<UserAvailabilityRedisDto?> GetAvailabilityAsync(Guid userId)
    {
        this.logger.LogInformation("Fetching availability for user {UserId}.", userId);
        var availability = await this.redisService.GetAvailabilityAsync(userId);
        if (availability == null)
        {
            this.logger.LogWarning("No availability found in cache for user {UserId}.", userId);
        }

        return availability;
    }

    /// <inheritdoc/>
    public async Task<UserAvailabilityUpdateResponse> PatchAvailabilityAsync(Guid userId, UserAvailabilityUpdateRequest availabilityUpdateRequest)
    {
        var ipAddress = this.httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = this.httpContextAccessor?.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";

        this.logger.LogInformation("User {UserId} is attempting to update availability. Status: {Status}, MaxConcurrentJobs: {MaxJobs}", userId, availabilityUpdateRequest.Status, availabilityUpdateRequest.MaxConcurrentJobs);

        var existingAvailability = await this.redisService.GetAvailabilityAsync(userId)
                                 ?? new UserAvailabilityRedisDto();

        if (!EnumDisplayHelper.TryParseDisplayName(availabilityUpdateRequest.Status, out UserAvailabilityType outcome))
        {
            this.logger.LogWarning("Rejecting availability update for user {UserId}: Invalid Status '{Status}'", userId, availabilityUpdateRequest.Status);
            throw new ArgumentException("Invalid Status Provided.");
        }

        if (availabilityUpdateRequest.MaxConcurrentJobs < 1)
        {
            this.logger.LogWarning("Rejecting availability update for user {UserId}: MaxConcurrentJobs must be > 0. Provided value: {Value}", userId, availabilityUpdateRequest.MaxConcurrentJobs);
            throw new ArgumentException("Maximum concurrent job should be greater than 0");
        }

        existingAvailability.Status = availabilityUpdateRequest.Status;
        existingAvailability.MaxConcurrentJobs = availabilityUpdateRequest.MaxConcurrentJobs;
        existingAvailability.LastUpdate = DateTime.UtcNow;

        // Asynchronously write to Redis and persist in DB
        var redisTask = this.redisService.SetAvailabilityAsync(userId, existingAvailability);
        var auditTask = this.UpdateAvailabilityAuditAsync(userId, existingAvailability, ipAddress, userAgent);

        await Task.WhenAll(redisTask, auditTask);

        var cloudEvent = new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri($"{TopicConstant.UserAvailabilityUpdated}:{userId}"),
            Type = TopicConstant.UserAvailabilityUpdated,
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json",
            Data = new { UserId = userId, Message = "User availability updated.", Data = existingAvailability },
        };

        try
        {
            await this.eventBus.PublishAsync(cloudEvent, TopicConstant.UserAvailabilityUpdated);
            this.logger.LogInformation("User {UserId} availability update - CloudEvent publish successful.", userId);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "User {UserId} availability update - CloudEvent publish failed.", userId);
        }

        this.logger.LogInformation("User {UserId} availability successfully updated. Status: {Status}, MaxConcurrentJobs: {MaxJobs}", userId, existingAvailability.Status, existingAvailability.MaxConcurrentJobs);

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
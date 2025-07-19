using AutoMapper;
using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Domain.Constants;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
using System.Text.Json;

namespace CohesionX.UserManagement.Modules.Users.Application.Services;

public class UserService : IUserService
{
	private readonly IUserRepository _repo;
	private readonly IAuditLogRepository _auditLogRepo;
	private readonly IEloService _eloService;
	private readonly IMapper _mapper;
	private readonly int _initElo;
	private readonly int _minEloRequiredForPro;
	private readonly int _minJobsRequiredForPro;

	public UserService(
		IUserRepository repo,
		IAuditLogRepository auditLogRepo,
		IMapper mapper,
		IConfiguration configuration,
		IEloService eloService)
	{
		_repo = repo;
		_auditLogRepo = auditLogRepo;
		_mapper = mapper;
		_eloService = eloService;
		var initEloStr = configuration["INITIAL_ELO_RATING"];
		if (!int.TryParse(initEloStr, out var initElo)) initElo = 360;
		_initElo = initElo;

		var initMinEloProStr = configuration["MIN_ELO_REQUIRED_FOR_PRO"];
		if (!int.TryParse(initMinEloProStr, out var minElo)) minElo = 1600;
		_minEloRequiredForPro = minElo;

		var initMinJobsProStr = configuration["MIN_JOBS_REQUIRED_FOR_PRO"];
		if (!int.TryParse(initMinJobsProStr, out var minJobs)) minJobs = 500;
		_minJobsRequiredForPro = minJobs;
	}

	public async Task<UserRegisterResponse> RegisterUserAsync(UserRegisterRequest dto)
	{
		// Validate required fields
		if (string.IsNullOrWhiteSpace(dto.FirstName) ||
			string.IsNullOrWhiteSpace(dto.LastName) ||
			string.IsNullOrWhiteSpace(dto.Email) ||
			string.IsNullOrWhiteSpace(dto.Phone) ||
			string.IsNullOrWhiteSpace(dto.IdNumber))
		{
			throw new ArgumentException("All required fields must be provided");
		}

		// Check if email already exists
		if (await _repo.EmailExistsAsync(dto.Email))
		{
			throw new ArgumentException("Email already registered");
		}

		// Create user entity
		var user = new User
		{
			Id = Guid.NewGuid(),
			FirstName = dto.FirstName,
			LastName = dto.LastName,
			Email = dto.Email,
			Phone = dto.Phone,
			IdNumber = dto.IdNumber,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow,
			Status = UserStatus.PENDING_VERIFICATION,
			Role = UserRole.TRANSCRIBER,
			IsProfessional = false
		};

		// Add dialect preferences
		foreach (var dialect in dto.DialectPreferences)
		{
			user.Dialects.Add(new UserDialect
			{
				Dialect = dialect,
				ProficiencyLevel = dto.LanguageExperience,
				IsPrimary = false,
				CreatedAt = DateTime.UtcNow
			});
		}

		user.Statistics = new UserStatistics
		{
			TotalJobs = 0,
			CurrentElo = _initElo,
			PeakElo = _initElo,
			GamesPlayed = 0,
			LastCalculated = DateTime.UtcNow,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		// Attempt activation
		var verificationRequired = new List<string>
		{
			"id_document_upload" 
		};

		await _repo.AddAsync(user);
		await _repo.SaveChangesAsync();

		return new UserRegisterResponse
		{
			UserId = user.Id,
			EloRating = user.Statistics.CurrentElo,
			Status = user.Status,
			ProfileUri= $"/api/v1/users/{user.Id}/profile",
			VerificationRequired = verificationRequired
		};
	}

	public async Task<UserProfileDto> GetProfileAsync(Guid userId)
	{
		var user = await _repo.GetUserByIdAsync(userId, includeRelated: true);
		if (user == null) throw new KeyNotFoundException("User not found");

		var stats = user.Statistics;
		var eloHistories = user.EloHistories;
		var dialects = user.Dialects;
		var jobCompletions = user.JobCompletions;

		var currentElo = stats?.CurrentElo ?? 0;
		var totalJobs = stats?.TotalJobs ?? 0;

		var missingCriteria = GetMissingCriteria(currentElo, totalJobs);
		var eligible = missingCriteria.Count == 0;

		var eloTrend7 = _eloService.GetEloTrend(eloHistories.ToList(), 7);
		var eloTrend30 = _eloService.GetEloTrend(eloHistories.ToList(), 30);
		var winRate = _eloService.GetWinRate(eloHistories.ToList());

		var jobsLast30Days = jobCompletions.Count(jc => jc.CompletedAt >= DateTime.UtcNow.AddDays(-30));

		var dto = new UserProfileDto
		{
			FirstName = user.FirstName,
			LastName = user.LastName,
			EloRating = currentElo,
			PeakElo = stats?.PeakElo ?? 0,
			Status = user.Status,
			RegisteredAt = user.CreatedAt,
			IsProfessional = user.Role == UserRole.PROFESSIONAL,
			ProfessionalEligibility = new ProfessionalEligibilityDto
			{
				Eligible = eligible,
				MissingCriteria = missingCriteria,
				Progress = new ProfessionalProgressDto
				{
					EloProgress = $"{currentElo}/{_minEloRequiredForPro}",
					JobsProgress = $"{totalJobs}/{_minJobsRequiredForPro}"
				}
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
					Earnings = 0 // TODO: Calculate
				}
			},
			Preferences = new UserPreferencesDto
			{
				MaxConcurrentJobs = 3,
				DialectPreferences = new List<string> { "western_cape" },
				PreferredJobTypes = new List<string> { "conference_calls", "interviews" }
			}
		};

		return dto;
	}


	public async Task<User> GetUserAsync(Guid userId)
	{
		var user = await _repo.GetUserByIdAsync(userId, includeRelated: true);
		if (user == null) throw new KeyNotFoundException("User not found");

		return user;
	}

	public async Task<List<User>> GetFilteredUser(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit)
	{
		var users = await _repo.GetFilteredUser(dialect, minElo, maxElo, maxWorkload, limit);
		return users;
	}

	public async Task UpdateAvailabilityAuditAsync(Guid userId, UserAvailabilityRedisDto existingAvailability, string? ipAddress, string? userAgent)
	{
		await _auditLogRepo.UpdateUserAvailabilityAuditLog(userId, existingAvailability, ipAddress, userAgent);
	}

	public async Task<VerificationResponse> ActivateUser(User user, VerificationRequest verificationDto)
	{
		user.Status = UserStatus.ACTIVE;
		var verificationRecord = new VerificationRecord
		{
			VerificationType = verificationDto.VerificationType,
			Status = VerificationStatus.APPROVED,
			VerificationLevel = VerificationLevel.BASIC,
			VerificationData = JsonSerializer.Serialize(verificationDto),
			VerifiedAt = DateTime.UtcNow,
			CreatedAt = DateTime.UtcNow
		};
		user.VerificationRecords.Add(verificationRecord);
		_repo.Update(user);

		var response = new VerificationResponse
		{
			VerificationStatus = verificationRecord.Status,
			EloRating = user.Statistics?.CurrentElo ?? 0,
			StatusChanged = "pending_verification -> active",
			EligibleForWork = true,
			ActivationMethod = "automatic",
			ActivatedAt = DateTime.UtcNow,
			VerificationLevel = verificationRecord.VerificationLevel,
			NextSteps = new[] { "profile_completion", "job_browsing" },
			RoadmapNote = "V2 will include automated ID verification via Department of Home Affairs"
		};
		return response;
	}

	public async Task<bool> CheckIdNumber(Guid userId, string idNumber)
	{
		var user = await _repo.GetUserByIdAsync(userId);
		if(user == null) return false;
		return user.IdNumber == idNumber;
	}

	private List<string> GetMissingCriteria(int elo, int totalJobs)
	{
		List<string> missingCriteria = [];
		if (elo < _minEloRequiredForPro) missingCriteria.Add("elo_rating");
		if (totalJobs < _minJobsRequiredForPro) missingCriteria.Add("total_jobs");

		return missingCriteria;
	}
}
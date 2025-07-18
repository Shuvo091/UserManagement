using AutoMapper;
using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Domain.Constants;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
using CohesionX.UserManagement.Modules.Users.Persistence;
using CohesionX.UserManagement.Shared.Constants;
using Microsoft.Extensions.Caching.Distributed;
using System.Linq.Expressions;
using System.Text.Json;

namespace CohesionX.UserManagement.Modules.Users.Application.Services;

public class UserService : IUserService
{
	private readonly IUserRepository _repo;
	private readonly IMapper _mapper;
	private readonly IPasswordHasher _passwordHasher;
	private readonly IDistributedCache _cache;
	private readonly int _initElo;

	public UserService(
		IUserRepository repo,
		IMapper mapper,
		IPasswordHasher passwordHasher,
		IDistributedCache cache,
		IConfiguration configuration)
	{
		_repo = repo;
		_mapper = mapper;
		_passwordHasher = passwordHasher;
		_cache = cache;
		var initEloStr = configuration["INITIAL_ELO_RATING"];
		if (!int.TryParse(initEloStr, out var initElo)) initElo = 360;
		_initElo = initElo;
	}

	public async Task<RegistrationResult> RegisterUserAsync(UserRegisterDto dto)
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

		return new RegistrationResult
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

		return _mapper.Map<UserProfileDto>(user);
	}

	public async Task<List<User>> GetFilteredUser(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit)
	{
		var users = await _repo.GetFilteredUser(dialect, minElo, maxElo, maxWorkload, limit);
		return users;
	}

}
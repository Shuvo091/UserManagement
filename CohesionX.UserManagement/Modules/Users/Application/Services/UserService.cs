using AutoMapper;
using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
using CohesionX.UserManagement.Modules.Users.Persistence;
using CohesionX.UserManagement.Shared.Constants;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CohesionX.UserManagement.Modules.Users.Application.Services;

public class UserService : IUserService
{
	private readonly IUserRepository _repo;
	private readonly IMapper _mapper;
	private readonly IPasswordHasher _passwordHasher;
	private readonly IDistributedCache _cache;

	public UserService(
		IUserRepository repo,
		IMapper mapper,
		IPasswordHasher passwordHasher,
		IDistributedCache cache)
	{
		_repo = repo;
		_mapper = mapper;
		_passwordHasher = passwordHasher;
		_cache = cache;
	}

	public async Task<RegistrationResult> RegisterUserAsync(UserRegisterDto dto, string? idPhotoPath)
	{
		// Validate required fields
		if (string.IsNullOrWhiteSpace(dto.FirstName) ||
			string.IsNullOrWhiteSpace(dto.LastName) ||
			string.IsNullOrWhiteSpace(dto.Email) ||
			string.IsNullOrWhiteSpace(dto.Phone) ||
			string.IsNullOrWhiteSpace(dto.IdNumber) ||
			string.IsNullOrWhiteSpace(dto.Password))
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
			Status = "pending_verification"
		};

		// Add dialect preferences
		foreach (var dialect in dto.DialectPreferences)
		{
			user.Dialects.Add(new UserDialect {
				Dialect = dialect,
				ProficiencyLevel = dto.LanguageExperience,
				IsPrimary = false,
				CreatedAt = DateTime.UtcNow
			});
		}

		// Attempt activation
		var verificationRequired = new List<string>();
		verificationRequired.Add("id_document_upload");

		await _repo.AddAsync(user);
		await _repo.SaveChangesAsync();

		return new RegistrationResult
		{
			UserId = user.Id,
			Status = user.Status,
			VerificationRequired = verificationRequired
		};
	}

	public async Task<UserProfileDto> GetProfileAsync(Guid userId)
	{
		var user = await _repo.GetUserByIdAsync(userId, includeRelated: true);
		if (user == null) throw new KeyNotFoundException("User not found");

		return _mapper.Map<UserProfileDto>(user);
	}

	public async Task<List<UserWithEloAndDialectsDto>> GetUsersWithDialect()
	{
		var users = await _repo.GetUsersWithEloAndDialectsAsync();
		return users.Select(u => new UserWithEloAndDialectsDto
		{
			UserId = u.UserId,
			DialectCodes = u.DialectCodes.Select(d => d).ToList(),
			EloRating = u.EloRating,
			PeakElo = u.PeakElo,
			GamesPlayed = u.GamesPlayed,
			IsProfessional = u.IsProfessional
		}).ToList();
	}

}

public class RegistrationResult
{
	public Guid UserId { get; set; }
	public string Status { get; set; }
	public List<string> VerificationRequired { get; set; } = new();
}
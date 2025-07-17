using AutoMapper;
using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
using CohesionX.UserManagement.Modules.Users.Persistence;

namespace CohesionX.UserManagement.Modules.Users.Application.Services;

public class UserService : IUserService
{
	private readonly IUserRepository _repo;
	private readonly IMapper _mapper;

	public UserService(IUserRepository repo, IMapper mapper)
	{
		_repo = repo;
		_mapper = mapper;
	}

	public async Task<Guid> RegisterUserAsync(UserRegisterDto dto)
	{
		// Basic field validation
		if (string.IsNullOrWhiteSpace(dto.IdNumber) ||
			string.IsNullOrWhiteSpace(dto.FirstName) ||
			string.IsNullOrWhiteSpace(dto.LastName) ||
			string.IsNullOrWhiteSpace(dto.Email) ||
			string.IsNullOrWhiteSpace(dto.PhoneNumber))
			throw new ArgumentException("All fields are required.");

		// South African ID format check (basic: 13 digits)
		if (dto.IdNumber.Length != 13 || !dto.IdNumber.All(char.IsDigit))
			throw new ArgumentException("ID number must be 13 digits.");

		var now = DateTime.UtcNow;

		var user = new User
		{
			Id = Guid.NewGuid(),
			IdNumber = dto.IdNumber,
			FirstName = dto.FirstName,
			LastName = dto.LastName,
			Email = dto.Email,
			Phone = dto.PhoneNumber,
			EloRating = 1200, // Initial Elo
			PeakElo = 1200,
			Status = "pending_verification",
			IsProfessional = false,
			CreatedAt = now,
			UpdatedAt = now
		};

		await _repo.AddAsync(user);
		await _repo.SaveChangesAsync();

		return user.Id;
	}


	public async Task<UserProfileDto> GetProfileAsync(Guid userId)
	{
		var u = await _repo.GetUserByIdAsync(userId, includeRelated: true);
		if (u is null) throw new KeyNotFoundException("User not found");

		return _mapper.Map<UserProfileDto>(u);
	}

}

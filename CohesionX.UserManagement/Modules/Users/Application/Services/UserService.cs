using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
using CohesionX.UserManagement.Modules.Users.Persistence;

namespace CohesionX.UserManagement.Modules.Users.Application.Services;

public class UserService : IUserService
{
	private readonly IUserRepository _repo;

	public UserService(IUserRepository repo)
	{
		_repo = repo;
	}

	public async Task<Guid> RegisterUserAsync(UserRegisterDto dto)
	{
		var user = new User
		{
			Id = Guid.NewGuid(),
			IdNumber = dto.IdNumber,
			FirstName = dto.FirstName,
			LastName = dto.LastName,
			Email = dto.Email,
			PhoneNumber = dto.PhoneNumber
		};

		await _repo.AddAsync(user);
		await _repo.SaveChangesAsync();

		return user.Id;
	}
}

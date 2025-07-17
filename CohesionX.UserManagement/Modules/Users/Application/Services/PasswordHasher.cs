using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CohesionX.UserManagement.Modules.Users.Application.Services;

public class PasswordHasher : IPasswordHasher
{
	private readonly PasswordHasher<User> _hasher = new();

	public string HashPassword(string password)
	{
		return _hasher.HashPassword(null, password);
	}

	public bool VerifyPassword(string hashedPassword, string providedPassword)
	{
		var result = _hasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
		return result == PasswordVerificationResult.Success;
	}
}
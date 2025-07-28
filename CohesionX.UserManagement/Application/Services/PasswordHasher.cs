using CohesionX.UserManagement.Application.Interfaces;
using CohesionX.UserManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CohesionX.UserManagement.Application.Services;

/// <summary>
/// Provides password hashing and verification operations using ASP.NET Core Identity.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
	private readonly PasswordHasher<User> _hasher = new();

	/// <summary>
	/// Hashes the specified plain-text password.
	/// </summary>
	/// <param name="password">The plain-text password to hash.</param>
	/// <returns>The hashed password string.</returns>
	public string HashPassword(string password)
	{
		return _hasher.HashPassword(null, password);
	}

	/// <summary>
	/// Verifies a provided password against a hashed password.
	/// </summary>
	/// <param name="hashedPassword">The previously hashed password.</param>
	/// <param name="providedPassword">The plain-text password to verify.</param>
	/// <returns><c>true</c> if the password matches; otherwise, <c>false</c>.</returns>
	public bool VerifyPassword(string hashedPassword, string providedPassword)
	{
		var result = _hasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
		return result == PasswordVerificationResult.Success;
	}
}

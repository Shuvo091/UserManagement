using CohesionX.UserManagement.Application.Interfaces;
using CohesionX.UserManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CohesionX.UserManagement.Application.Services;

/// <summary>
/// Provides password hashing and verification operations using ASP.NET Core Identity.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
	private readonly PasswordHasher<User> _hasher = new ();

	/// <summary>
	/// Hashes the specified plain-text password.
	/// </summary>
	/// <param name="user">User to hash.</param>
	/// <param name="password">The plain-text password to hash.</param>
	/// <returns>The hashed password string.</returns>
	public string HashPassword(User user, string password)
	{
		return _hasher.HashPassword(user, password);
	}

	/// <summary>
	/// Verifies a provided password against a hashed password.
	/// </summary>
	/// <param name="user">The user to be checked.</param>
	/// <param name="hashedPassword">The previously hashed password.</param>
	/// <param name="providedPassword">The plain-text password to verify.</param>
	/// <returns><c>true</c> if the password matches; otherwise, <c>false</c>.</returns>
	public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
	{
		var result = _hasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
		return result == PasswordVerificationResult.Success;
	}
}

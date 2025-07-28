namespace CohesionX.UserManagement.Application.Interfaces;

/// <summary>
/// Provides password hashing and verification operations.
/// </summary>
public interface IPasswordHasher
{
	/// <summary>
	/// Hashes the specified plain-text password.
	/// </summary>
	/// <param name="password">The plain-text password to hash.</param>
	/// <returns>The hashed password string.</returns>
	string HashPassword(string password);

	/// <summary>
	/// Verifies a provided password against a hashed password.
	/// </summary>
	/// <param name="hashedPassword">The previously hashed password.</param>
	/// <param name="providedPassword">The plain-text password to verify.</param>
	/// <returns><c>true</c> if the password matches; otherwise, <c>false</c>.</returns>
	bool VerifyPassword(string hashedPassword, string providedPassword);
}

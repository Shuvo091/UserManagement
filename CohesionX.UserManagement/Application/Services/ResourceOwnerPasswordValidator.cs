using System.Security.Claims;
using CohesionX.UserManagement.Application.Interfaces;
using CohesionX.UserManagement.Domain.Entities;
using IdentityServer4.Models;
using IdentityServer4.Validation;

namespace CohesionX.UserManagement.Application.Services;

/// <summary>
/// Validates resource owner password credentials for IdentityServer4 authentication.
/// </summary>
public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
{
	private readonly IUserService _userService;
	private readonly IPasswordHasher _passwordHasher;
	private readonly ILogger<ResourceOwnerPasswordValidator> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="ResourceOwnerPasswordValidator"/> class.
	/// </summary>
	/// <param name="userService">Service for user management operations.</param>
	/// <param name="passwordHasher">Service for password hashing and verification.</param>
	/// <param name="logger"> logger. </param>
	public ResourceOwnerPasswordValidator(IUserService userService, IPasswordHasher passwordHasher, ILogger<ResourceOwnerPasswordValidator> logger)
	{
		_userService = userService;
		_passwordHasher = passwordHasher;
		_logger = logger;
	}

	/// <summary>
	/// Validates the resource owner password credentials and sets the authentication result.
	/// </summary>
	/// <param name="context">The validation context containing username and password.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
	{
		var user = await _userService.GetUserByEmailAsync(context.UserName);

		if (user == null)
		{
			context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid username or password");
			return;
		}

		if (!_passwordHasher.VerifyPassword(user, context.Password, user.PasswordHash))
		{
			context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid username or password");
			return;
		}

		// Success - set subject and claims
		context.Result = new GrantValidationResult(
			subject: user.Id.ToString(),
			authenticationMethod: "custom",
			claims: GetUserClaims(user));
	}

	/// <summary>
	/// Gets the claims for the authenticated user.
	/// </summary>
	/// <param name="user">The authenticated user entity.</param>
	/// <returns>A collection of claims for the user.</returns>
	private IEnumerable<Claim> GetUserClaims(User user)
	{
		return new List<Claim>
		{
			new Claim("sub", user.Id.ToString()),
			new Claim("email", user.Email),
			new Claim("name", $"{user.FirstName} {user.LastName}"),
			new Claim("role", user.Role),
		};
	}
}

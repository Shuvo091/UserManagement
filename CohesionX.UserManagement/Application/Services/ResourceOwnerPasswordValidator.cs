using CohesionX.UserManagement.Application.Interfaces;
using CohesionX.UserManagement.Domain.Entities;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using System.Security.Claims;

namespace CohesionX.UserManagement.Application.Services;

public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
{
	private readonly IUserService _userService;
	private readonly IPasswordHasher _passwordHasher;

	public ResourceOwnerPasswordValidator(IUserService userService, IPasswordHasher passwordHasher)
	{
		_userService = userService;
		_passwordHasher = passwordHasher;
	}

	public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
	{
		var user = await _userService.GetUserByEmailAsync(context.UserName);

		if (user == null)
		{
			context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid username or password");
			return;
		}

		if (!_passwordHasher.VerifyPassword(context.Password, user.PasswordHash))
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

using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CohesionX.UserManagement.Modules.Users.Api;

public static class UserEndpoints
{
	public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/api/v1/users").WithTags("Users");

		// Registration endpoint removed (now handled by UsersController)

		return endpoints;
	}
}
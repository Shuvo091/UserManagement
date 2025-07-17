using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Application.Services;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;

namespace CohesionX.UserManagement.Modules.Users.Api;

public static class UserEndpoints
{
	public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/api/v1/users").WithTags("Users");

		group.MapPost("/register", async (UserRegisterDto dto, IUserService service) =>
		{
			var userId = await service.RegisterUserAsync(dto);
			return Results.Created($"/api/v1/users/{userId}/profile", new
			{
				userId,
				eloRating = 1200,
				status = "pending_verification",
				profileUri = $"/api/v1/users/{userId}/profile",
				verificationRequired = new[] { "id_document_upload" }
			});
		});

		return endpoints;
	}
}

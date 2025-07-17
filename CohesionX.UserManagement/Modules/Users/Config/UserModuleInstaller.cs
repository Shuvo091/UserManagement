using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Application.Services;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
using CohesionX.UserManagement.Modules.Users.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace CohesionX.UserManagement.Modules.Users.Config;

public static class UserModuleInstaller
{
	public static IServiceCollection RegisterUserModule(this IServiceCollection services)
	{
		services.AddScoped<IUserService, UserService>();
		services.AddScoped<IUserRepository, UserRepository>();
		return services;
	}
}

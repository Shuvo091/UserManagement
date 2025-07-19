using CohesionX.UserManagement.Modules.Elo.Application.Services;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Application.Services;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
using CohesionX.UserManagement.Modules.Users.Persistence;
using CohesionX.UserManagement.Shared.Persistence;

namespace CohesionX.UserManagement.Modules.Users.Config;

public static class UserModuleInstaller
{
	public static IServiceCollection RegisterUserModule(this IServiceCollection services)
	{
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<IEloRepository, EloRepository>();
		services.AddScoped<IAuditLogRepository, AuditLogRepository>();
		services.AddScoped<IUserStatisticsRepository, UserStatisticsRepository>();

		services.AddScoped<IPasswordHasher, PasswordHasher>();
		services.AddScoped<IFileStorageService, FileStorageService>();

		services.AddScoped<IUserService, UserService>();
		services.AddScoped<IEloService, EloService>();
		services.AddScoped<IRedisService, RedisService>();
		services.AddScoped<IUserStaticticsService, UserStatisticsService>();

		services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
		services.AddScoped<IUnitOfWork, UnitOfWork>();
		return services;
	}
}

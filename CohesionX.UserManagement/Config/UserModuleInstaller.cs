using CohesionX.UserManagement.Application.Interfaces;
using CohesionX.UserManagement.Application.Services;
using CohesionX.UserManagement.Persistence;
using CohesionX.UserManagement.Persistence.Interfaces;
using IdentityServer4.Validation;

namespace CohesionX.UserManagement.Config;

public static class UserModuleInstaller
{
	public static IServiceCollection RegisterUserModule(this IServiceCollection services)
	{
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<IEloRepository, EloRepository>();
		services.AddScoped<IAuditLogRepository, AuditLogRepository>();
		services.AddScoped<IUserStatisticsRepository, UserStatisticsRepository>();
		services.AddScoped<IJobClaimRepository, JobClaimRepository>();
		services.AddScoped<IVerificationRequirementRepository, VerificationRequirementRepository>();

		services.AddScoped<IPasswordHasher, PasswordHasher>();
		services.AddScoped<IFileStorageService, FileStorageService>();

		services.AddScoped<IUserService, UserService>();
		services.AddScoped<IEloService, EloService>();
		services.AddScoped<IRedisService, RedisService>();
		services.AddScoped<IUserStaticticsService, UserStatisticsService>();
		services.AddScoped<IVerificationRequirementService, VerificationRequirementService>();

		services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
		services.AddScoped<IUnitOfWork, UnitOfWork>();

		services.AddTransient<IResourceOwnerPasswordValidator, ResourceOwnerPasswordValidator>();
		return services;
	}
}

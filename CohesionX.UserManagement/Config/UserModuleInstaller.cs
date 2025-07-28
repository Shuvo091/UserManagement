using CohesionX.UserManagement.Application.Interfaces;
using CohesionX.UserManagement.Application.Services;
using CohesionX.UserManagement.Persistence;
using CohesionX.UserManagement.Persistence.Interfaces;
using IdentityServer4.Validation;

namespace CohesionX.UserManagement.Config;

/// <summary>
/// Provides extension methods for registering user management module services in the dependency injection container.
/// </summary>
public static class UserModuleInstaller
{
	/// <summary>
	/// Registers all user management, repository, and related services required by the user module.
	/// </summary>
	/// <param name="services">The service collection to add registrations to.</param>
	/// <returns>The updated service collection.</returns>
	public static IServiceCollection RegisterUserModule(this IServiceCollection services)
	{
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<IEloRepository, EloRepository>();
		services.AddScoped<IAuditLogRepository, AuditLogRepository>();
		services.AddScoped<IUserStatisticsRepository, UserStatisticsRepository>();
		services.AddScoped<IJobClaimRepository, JobClaimRepository>();
		services.AddScoped<IVerificationRequirementRepository, VerificationRequirementRepository>();

		services.AddScoped<IPasswordHasher, PasswordHasher>();

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

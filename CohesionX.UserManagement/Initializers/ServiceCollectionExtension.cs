using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Initializers.TaskTracker;

namespace CohesionX.UserManagement.Initializers;

/// <summary>
/// Extension methods for registering seed initializer services.
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// Adds seed initializer services to the service collection and runs initialization.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSeedInitializerServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SeedFilePathsOptions>(configuration.GetSection(nameof(SeedFilePathsOptions)));

        services.AddHostedService<SeedInitializer>();

        services.AddSingleton<IStartupTaskTracker, StartupTaskTracker>();

        return services;
    }
}
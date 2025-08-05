using CohesionX.UserManagement.Abstractions.DTOs.Options;
using SharedLibrary.Cache.Models;
using SharedLibrary.Common.Options;

namespace CohesionX.UserManagement.Extensions;

/// <summary>
/// The configuration extensions class provides methods to configure application options from the provided configuration.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Configures application options using the provided configuration settings.
    /// </summary>
    /// <param name="services"> service to extend. </param>
    /// <param name="config"> base cofig. </param>
    /// <returns> returns augmented collection. </returns>
    public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<DatabasesOptions>(config.GetSection(nameof(DatabasesOptions)));
        services.Configure<RedisConfiguration>(config.GetSection(nameof(RedisConfiguration)));
        services.Configure<IdentityServerOptions>(config.GetSection(nameof(IdentityServerOptions)));
        services.Configure<WorkflowEngineOptions>(config.GetSection(nameof(WorkflowEngineOptions)));
        services.Configure<JwtOptions>(config.GetSection(nameof(JwtOptions)));
        services.Configure<AppConstantsOptions>(config.GetSection(nameof(AppConstantsOptions)));
        services.Configure<ValidationOptions>(config.GetSection(nameof(ValidationOptions)));

        return services;
    }
}

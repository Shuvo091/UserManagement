using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Database.Abstractions.Options;

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
        services.Configure<DbConnectionOptions>(config.GetSection("DB_CONNECTION_STRING"));
        services.Configure<RedisOptions>(config.GetSection("Redis"));
        services.Configure<IdentityServerOptions>(config.GetSection("IdentityServer"));
        services.Configure<WorkflowEngineOptions>(config.GetSection("WorkflowEngine"));
        services.Configure<DbSeederOptions>(config.GetSection("DbSeeder"));
        services.Configure<AppConstantsOptions>(options =>
        {
            options.EnableGrpc = config.GetValue<bool>("ENABLE_GRPC");
            options.EnableIdDocumentCollection = config.GetValue<bool>("ENABLE_ID_DOCUMENT_COLLECTION");
            options.PopiaComplianceMode = config["POPIA_COMPLIANCE_MODE"] !;
            options.InitialEloRating = config.GetValue<int>("INITIAL_ELO_RATING");
            options.MinEloRequiredForPro = config.GetValue<int>("MIN_ELO_REQUIRED_FOR_PRO");
            options.MinJobsRequiredForPro = config.GetValue<int>("MIN_JOBS_REQUIRED_FOR_PRO");
            options.EloKFactorNew = config.GetValue<int>("ELO_K_FACTOR_NEW");
            options.EloKFactorEstablished = config.GetValue<int>("ELO_K_FACTOR_ESTABLISHED");
            options.EloKFactorExpert = config.GetValue<int>("ELO_K_FACTOR_EXPERT");
            options.JobTimeoutHours = config.GetValue<int>("JOB_TIMEOUT_HOURS");
            options.MaxConcurrentJobs = config.GetValue<int>("MAX_CONCURRENT_JOBS");
            options.RedisCacheTtlMinutes = config.GetValue<int>("REDIS_CACHE_TTL_MINUTES");
            options.DefaultBookoutMinutes = config.GetValue<int>("DEFAULT_BOOKOUT_MINUTES");
        });

        return services;
    }
}

using CohesionX.UserManagement.Abstractions.DTOs.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CohesionX.UserManagement.Extensions;

/// <summary>
/// Extension methods for configuring authentication and authorization in the application.
/// </summary>
public static class AuthExtensions
{
    /// <summary>
    /// Registers JWT authentication with the dependency injection container.
    /// </summary>
    /// <param name="services"> service to extend. </param>
    /// <param name="config"> base cofig. </param>
    /// <returns> returns augmented collection. </returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var identity = config.GetSection("IdentityServer").Get<IdentityServerOptions>() !;
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = identity.Authority;
                options.Audience = identity.ApiName;
                options.RequireHttpsMetadata = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    RoleClaimType = "role",
                    NameClaimType = "name",
                };
            });
        return services;
    }

    /// <summary>
    /// Registers an authorization policy with the dependency injection container.
    /// </summary>
    /// <param name="services"> service to extend. </param>
    /// <param name="config"> base cofig. </param>
    /// <returns> returns augmented collection. </returns>
    public static IServiceCollection AddAuthorizationPolicy(this IServiceCollection services, IConfiguration config)
    {
        var identity = config.GetSection("IdentityServer").Get<IdentityServerOptions>() !;
        services.AddAuthorization(options =>
        {
            options.AddPolicy("ApiScope", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", identity.ApiName);
            });
        });
        return services;
    }
}

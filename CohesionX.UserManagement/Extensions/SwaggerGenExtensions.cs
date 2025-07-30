using CohesionX.UserManagement.Abstractions.DTOs.Options;
using Microsoft.OpenApi.Models;

namespace CohesionX.UserManagement.Extensions;

/// <summary>
/// Extension methods for configuring Swagger generation with OAuth support in ASP.NET Core applications.
/// </summary>
public static class SwaggerGenExtensions
{
	/// <summary>
	/// Registers Swagger generation services with OAuth2 support in the dependency injection container.
	/// </summary>
	/// <param name="services"> service to extend. </param>
	/// <param name="config"> base cofig. </param>
	/// <returns> returns augmented collection. </returns>
	public static IServiceCollection AddSwaggerGenWithOAuth(this IServiceCollection services, IConfiguration config)
	{
		var identity = config.GetSection("IdentityServer").Get<IdentityServerOptions>() !;
		services.AddSwaggerGen(options =>
		{
			options.SwaggerDoc("v1", new OpenApiInfo { Title = "User Management API", Version = "v1" });

			options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
			{
				Type = SecuritySchemeType.OAuth2,
				Description = "OAuth2 Authorization Code flow with PKCE",
				Flows = new OpenApiOAuthFlows
				{
					AuthorizationCode = new OpenApiOAuthFlow
					{
						AuthorizationUrl = new Uri($"{identity.Authority.TrimEnd('/')}/connect/authorize"),
						TokenUrl = new Uri($"{identity.Authority.TrimEnd('/')}/connect/token"),
						Scopes = new Dictionary<string, string>
						{
							{ "openid", "OpenID Connect" },
							{ "profile", "User profile" },
							{ "api", "Access CohesionX User Management API" },
							{ "offline_access", "Refresh Token Support" },
							{ "role", "Role-based authorization" },
						},
					},
				},
			});

			options.AddSecurityRequirement(new OpenApiSecurityRequirement
			{
				{
					new OpenApiSecurityScheme
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.SecurityScheme,
							Id = "oauth2",
						},
					},
					new[] { "openid", "profile", "api", "offline_access", "role" }
				},
			});
		});

		return services;
	}

	/// <summary>
	/// Configures Swagger UI with OAuth2 support in the ASP.NET Core application pipeline.
	/// </summary>
	/// <param name="app"> app to extend. </param>
	/// <param name="config"> base cofig. </param>
	public static void UseSwaggerUIWithOAuth(this IApplicationBuilder app, IConfiguration config)
	{
		var identity = config.GetSection("IdentityServer").Get<IdentityServerOptions>() !;
		app.UseSwaggerUI(options =>
		{
			options.OAuthClientId("swagger.api");
			options.OAuthUsePkce();
			options.OAuthScopeSeparator(" ");
			options.OAuth2RedirectUrl("https://localhost:7039/swagger/oauth2-redirect.html");
			options.OAuthAdditionalQueryStringParams(new Dictionary<string, string>
			{
				{ "audience", identity.ApiName },
			});
		});
	}
}

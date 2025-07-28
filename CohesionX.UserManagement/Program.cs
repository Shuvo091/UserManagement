using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SharedLibrary.Cache.ServiceCollectionExtensions;
using CohesionX.UserManagement.Middleware;
using CohesionX.UserManagement.Application.Interfaces;
using CohesionX.UserManagement.Application.Services;
using CohesionX.UserManagement.Config;
using CohesionX.UserManagement.Persistence;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

// Add services to the container
services.AddControllers();

// PostgreSQL DbContext
services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(config["DB_CONNECTION_STRING:db-secrets"]));

// JWT Authentication with Role-based Claims
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.Authority = config["IdentityServer:Authority"]; // e.g., "https://dev.vectormind.chat/security"
		options.Audience = config["IdentityServer:ApiName"];   // e.g., "api"
		options.RequireHttpsMetadata = true;

		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			RoleClaimType = "role",
			NameClaimType = "name"
		};
	});

// Authorization Policy with scope requirement
services.AddAuthorization(options =>
{
	options.AddPolicy("ApiScope", policy =>
	{
		policy.RequireAuthenticatedUser();
		policy.RequireClaim("scope", config["IdentityServer:ApiName"]!); // "api"
	});
});

// Swagger with JWT support
services.AddEndpointsApiExplorer();
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
				AuthorizationUrl = new Uri($"{config["IdentityServer:Authority"]!.TrimEnd('/')}/connect/authorize"),
				TokenUrl = new Uri($"{config["IdentityServer:Authority"]!.TrimEnd('/')}/connect/token"),
				Scopes = new Dictionary<string, string>
			{
				{ "openid", "OpenID Connect" },
				{ "profile", "User profile" },
				{ "api", "Access CohesionX User Management API" },
				{ "offline_access", "Refresh Token Support" },
				{ "role", "Role-based authorization" }
			}
			}
		}
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
{
	{
		new OpenApiSecurityScheme
		{
			Reference = new OpenApiReference
			{
				Type = ReferenceType.SecurityScheme,
				Id = "oauth2"
			}
		},
		new[] { "openid", "profile", "api", "offline_access", "role" }
	}
});

});


// Register modules
services.AddRedisCache(config);
services.RegisterUserModule();
builder.Services.AddHttpClient<IWorkflowEngineClient, WorkflowEngineClient>(client =>
{
	client.BaseAddress = new Uri(config["WORKFLOW_ENGINE_BASE_URI"]!);
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(options =>
	{
		options.OAuthClientId("swagger.api"); // Must match VectorMind client ID
		options.OAuthUsePkce();
		options.OAuthScopeSeparator(" ");
		options.OAuth2RedirectUrl("https://localhost:7039/swagger/oauth2-redirect.html");
		options.OAuthAdditionalQueryStringParams(new Dictionary<string, string>
		{
			{ "audience", config["Jwt:Audience"]! } // optional
		});
	});
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	context.Database.Migrate();
	await DbSeeder.SeedGlobalVerificationRequirementAsync(context);
}
app.Run();

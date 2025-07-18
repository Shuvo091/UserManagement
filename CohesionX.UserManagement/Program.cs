using CohesionX.UserManagement.Shared.Persistence;
using CohesionX.UserManagement.Modules.Users.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

// Add services to the container
services.AddControllers();

// PostgreSQL DbContext
services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(config.GetConnectionString("Postgres")));

// Redis (optional for availability cache)
services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = config.GetConnectionString("Redis");
});

// Authentication: JWT Bearer with Role-based claims
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.Authority = config["Jwt:Authority"] ?? "https://identityserver.example.com"; // Set your IdentityServer URL in config
		options.Audience = config["Jwt:Audience"] ?? "CohesionX.Users";
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			RoleClaimType = "role"
		};
	});

services.AddAuthorization();

// Swagger with JWT support
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo { Title = "User Management API", Version = "v1" });
	options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
	{
		Type = SecuritySchemeType.OAuth2,
		Flows = new OpenApiOAuthFlows
		{
			AuthorizationCode = new OpenApiOAuthFlow
			{
				AuthorizationUrl = new Uri(config["Jwt:Authority"] + "/connect/authorize"),
				TokenUrl = new Uri(config["Jwt:Authority"] + "/connect/token"),
				Scopes = new Dictionary<string, string>
				{
					{ config["Jwt:Audience"], "Access CohesionX User Management API" }
				}
			}
		}
	});
	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" } },
			new string[] { config["Jwt:Audience"] }
		}
	});
});

// Register modules
services.RegisterUserModule();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Minimal endpoints
// app.MapUserEndpoints();

app.MapControllers();

app.Run();

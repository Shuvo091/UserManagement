using System.Text;
using CohesionX.UserManagement.Modules.Users.Api;
using CohesionX.UserManagement.Shared.Persistence;
using CohesionX.UserManagement.Modules.Users.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;

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
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = config["Jwt:Issuer"],
			ValidAudience = config["Jwt:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!))
		};
	});

services.AddAuthorization();

// Swagger with JWT support
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo { Title = "User Management API", Version = "v1" });
	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		In = ParameterLocation.Header,
		Description = "JWT Authorization header using the Bearer scheme",
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "Bearer"
	});
	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
			new string[] {}
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
app.MapUserEndpoints();

app.Run();

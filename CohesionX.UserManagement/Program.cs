using CohesionX.UserManagement.Application.Interfaces;
using CohesionX.UserManagement.Application.Services;
using CohesionX.UserManagement.Config;
using CohesionX.UserManagement.Extensions;
using CohesionX.UserManagement.Middleware;
using CohesionX.UserManagement.Services;
using Serilog;
using SharedLibrary.Cache.ServiceCollectionExtensions;

Log.Logger = new LoggerConfiguration()
	.Enrich.FromLogContext()
	.WriteTo.Console()
	.WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
	.CreateLogger();

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// 1. Controllers & Swagger
services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGenWithOAuth(configuration);

// 2. Configuration binding
services.ConfigureOptions(configuration);

// 4. Custom modules
builder.Services.AddRedis(configuration);
builder.Services.AddRedisCache(configuration);
services.RegisterUserModule();
builder.Services.AddHttpClient<IWorkflowEngineClient, WorkflowEngineClient>();
services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
services.AddHostedService<MigrationAndSeedingService>();

// 5. DB + Auth + Policies
services.AddAppDbContext(configuration);
services.AddJwtAuthentication(configuration);
services.AddAuthorizationPolicy(configuration);
builder.Host.UseSerilog();

var app = builder.Build();

// Swagger UI with OAuth2 (in dev/docker only)
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
	app.UseSwagger();
	app.UseSwaggerUIWithOAuth(configuration);
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

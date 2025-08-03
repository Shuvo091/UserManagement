using CohesionX.UserManagement.Abstractions.Services;
using CohesionX.UserManagement.Application.Extensions;
using CohesionX.UserManagement.Application.Services;
using CohesionX.UserManagement.Config;
using CohesionX.UserManagement.Database.Services;
using CohesionX.UserManagement.Extensions;
using CohesionX.UserManagement.Middleware;
using Prometheus;
using SharedLibrary.Cache.ServiceCollectionExtensions;
using SharedLibrary.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var host = builder.Host;

// 1. Controllers & Swagger
services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGenWithJwt();

// 2. Configuration binding
services.ConfigureOptions(configuration);

// 4. Custom modules
services.AddRedis(configuration);
services.AddRedisCache();
services.RegisterUserModule();
services.AddHttpClient<IWorkflowEngineClient, WorkflowEngineClient>();
services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
services.AddHostedService<MigrationAndSeedingService>();

// 5. DB + Auth + Policies
services.AddAppDbContext(configuration);
services.AddJwtAuthentication(configuration);
services.AddAuthorization();

// 6. Logger
host.AddSerilogLogging();

// Health
services.AddHealthChecks();
services.RegisterOpenTelemetry(configuration);

var app = builder.Build();

// Swagger UI with OAuth2 (in dev/docker only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUIWithJwt();
}

// Collect default HTTP metrics and Expose /metrics endpoint for Prometheus
app.UseHttpMetrics();
app.MapPrometheusScrapingEndpoint("/metrics");

// Expose health endpoint
app.MapHealthChecks("/health");

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

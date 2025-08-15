using CohesionX.UserManagement.Abstractions.Services;
using CohesionX.UserManagement.Application.Extensions;
using CohesionX.UserManagement.Application.Services;
using CohesionX.UserManagement.Config;
using CohesionX.UserManagement.Extensions;
using CohesionX.UserManagement.Initializers;
using Polly;
using Polly.Extensions.Http;
using Prometheus;
using SharedLibrary.Cache.ServiceCollectionExtensions;
using SharedLibrary.Common.ExceptionMiddlewares;
using SharedLibrary.Common.Extensions;
using SharedLibrary.Kafka.ServiceCollectionExtensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var host = builder.Host;
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(30);

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(6, TimeSpan.FromSeconds(30));

// 1. Controllers & Swagger
services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGenWithJwt();

// 2. Configuration binding
services.ConfigureOptions(configuration);

// 3. Httpcontext accessor
services.AddHttpContextAccessor();

// 4. Custom modules
services.AddRedis(configuration);
services.AddRedisCache();
services.AddKafka(configuration);
services.RegisterUserModule();
services.AddHttpClient<IWorkflowEngineClient, WorkflowEngineClient>()
            .AddHttpMessageHandler<WorkflowEngineAuthHandler>()
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(timeoutPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);
services.AddTransient<WorkflowEngineAuthHandler>();

// 5. DB + Auth + Policies
services.AddAppDbContext(configuration);
services.AddSeedInitializerServices(configuration);
services.AddJwtAuthentication(configuration);
services.AddAuthorization();

// 6. Logger
host.AddSerilogLogging();

// Health
services.AddCustomHealthChecks(configuration);

// OpenTelemetry
services.RegisterOpenTelemetry(configuration);

var app = builder.Build();

// Swagger UI with OAuth2 (in dev/docker only)
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
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

using CohesionX.UserManagement.Application.Exceptions;
using Serilog.Context;
namespace CohesionX.UserManagement.Middleware;

/// <summary>
/// Global middleware for capturing unhandled exceptions and logging them.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionMiddleware> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next"> next. </param>
    /// <param name="logger"> logger. </param>
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to handle exceptions globally in the application.
    /// </summary>
    /// <param name="context"> context. </param>
    /// <returns>Task. </returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.TraceIdentifier;
        var tenantId = context.Request.Headers.TryGetValue("X-Tenant-ID", out var tid) ? tid.ToString() : "unknown";
        var userId = context.User?.FindFirst("sub")?.Value ?? context.User?.Identity?.Name ?? "anonymous";

        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("TenantId", tenantId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("HttpMethod", context.Request.Method))
        {
            try
            {
                await this.next(context);
            }
            catch (CustomException customEx)
            {
                this.logger.LogWarning(customEx, "Handled custom exception: {Message}", customEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Unhandled exception occurred.");
                throw;
            }
        }
    }
}

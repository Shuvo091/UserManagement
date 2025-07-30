using System.Net;
using System.Text.Json;
using CohesionX.UserManagement.Infrastucture;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CohesionX.UserManagement.Middleware;

/// <summary>
/// Global middleware for capturing unhandled exceptions and logging them.
/// </summary>
public class ExceptionMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<ExceptionMiddleware> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
	/// </summary>
	/// <param name="next"> next. </param>
	/// <param name="logger"> logger. </param>
	public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	/// <summary>
	/// Invokes the middleware to handle exceptions globally in the application.
	/// </summary>
	/// <param name="context"> context. </param>
	/// <returns>Task. </returns>
	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		catch (CustomException customEx)
		{
			_logger.LogWarning(customEx, "Handled exception: {Message}", customEx.Message);
			await HandleExceptionAsync(context, customEx.StatusCode, customEx.Message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unhandled exception occurred.");
			await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
		}
	}

	private static Task HandleExceptionAsync(HttpContext context, HttpStatusCode code, string message)
	{
		context.Response.ContentType = "application/json";
		context.Response.StatusCode = (int)code;

		var response = new
		{
			error = message,
			statusCode = (int)code,
		};

		return context.Response.WriteAsync(JsonSerializer.Serialize(response));
	}
}

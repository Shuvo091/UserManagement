namespace CohesionX.UserManagement.Initializers.TaskTracker;

/// <summary>
/// Middleware that ensures all startup tasks are completed before processing the HTTP request.
/// </summary>
public class StartupTaskTrackerMiddleware
{
    private readonly RequestDelegate next;
    private readonly IStartupTaskTracker startupTaskTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupTaskTrackerMiddleware"/> class.
    /// </summary>
    /// <param name="next">The delegate representing the next middleware in the pipeline.</param>
    /// <param name="startupTaskTracker">The tracker used to monitor the progress and status of startup tasks.</param>
    public StartupTaskTrackerMiddleware(RequestDelegate next, IStartupTaskTracker startupTaskTracker)
    {
        this.next = next;
        this.startupTaskTracker = startupTaskTracker;
    }

    /// <summary>
    /// Invokes the next middleware in the pipeline after ensuring that all startup tasks have completed.
    /// </summary>
    /// <remarks>This method ensures that all registered startup tasks are completed before proceeding to the
    /// next middleware. It is typically used in middleware components to enforce startup task completion before
    /// handling requests.</remarks>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        await this.startupTaskTracker.WhenStartupTasksCompletedAsync();
        await this.next(context);
    }
}
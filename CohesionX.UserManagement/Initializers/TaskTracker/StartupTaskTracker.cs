namespace CohesionX.UserManagement.Initializers.TaskTracker;

/// <inheritdoc/>
public class StartupTaskTracker : IStartupTaskTracker
{
    private readonly TaskCompletionSource<bool> taskCompletionSource = new ();

    /// <inheritdoc/>
    public void MarkCompleted()
    {
        this.taskCompletionSource.TrySetResult(true);
    }

    /// <inheritdoc/>
    public Task WhenStartupTasksCompletedAsync()
    {
        return this.taskCompletionSource.Task;
    }
}
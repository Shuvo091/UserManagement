namespace CohesionX.UserManagement.Initializers.TaskTracker;

/// <summary>
/// Tracks the completion status of startup tasks and provides a mechanism to await their completion.
/// </summary>
public interface IStartupTaskTracker
{
    /// <summary>
    /// Returns a task that completes when all startup tasks have finished executing.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task completes when all startup tasks are
    /// finished.</returns>
    Task WhenStartupTasksCompletedAsync();

    /// <summary>
    /// Marks the current task as completed.
    /// </summary>
    void MarkCompleted();
}
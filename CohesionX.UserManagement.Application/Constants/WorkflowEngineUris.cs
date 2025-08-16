namespace CohesionX.UserManagement.Application.Constants;

/// <summary>
/// Provides URI endpoints for Workflow Engine API operations.
/// </summary>
public static class WorkflowEngineUris
{
    /// <summary>
    /// Gets the endpoint for notifying the Workflow Engine about a updated elo.
    /// </summary>
    public static string NotifyEloUpdate => "/webhook/user-events";
}

namespace CohesionX.UserManagement.Application.Constants;

/// <summary>
/// The topic constants.
/// </summary>
public static class TopicConstant
{
    /// <summary>
    /// Triggers workflow engine to finalise job workflow.
    /// </summary>
    public const string UserEloUpdated = "user.elo.updated";

    /// <summary>
    /// Audit trail only.
    /// </summary>
    public const string UserAvailabilityUpdated = "user.availability.updated";

    /// <summary>
    /// Audit trail only.
    /// </summary>
    public const string UserPerformanceMilestone = "user.performance.milestone";
}

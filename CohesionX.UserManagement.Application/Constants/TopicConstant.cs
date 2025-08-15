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
    /// Triggers when user's availability is updated.
    /// </summary>
    public const string UserAvailabilityUpdated = "user.availability.updated";

    /// <summary>
    /// Triggers when user's performance hits a milestone.
    /// </summary>
    public const string UserPerformanceMilestone = "user.performance.milestone";

    /// <summary>
    /// Triggers when user's performance hits a milestone.
    /// </summary>
    public const string UserJobClaimed = "user.job.claimed";
}

using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Abstractions.Services;

/// <summary>
/// Provides operations for notifying the workflow engine about Elo updates.
/// </summary>
public interface IWorkflowEngineClient
{
	/// <summary>
	/// Notifies the workflow engine that an Elo update has occurred.
	/// </summary>
	/// <param name="request">The Elo update notification request details.</param>
	/// <returns>
	/// The response from the workflow engine acknowledging the update,
	/// or <c>null</c> if no response was received.
	/// </returns>
	Task<EloUpdateNotificationResponse?> NotifyEloUpdatedAsync(EloUpdateNotificationRequest request);
}

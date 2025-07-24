using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Application.Interfaces;

public interface IWorkflowEngineClient
{
	Task<EloUpdateNotificationResponse?> NotifyEloUpdatedAsync(EloUpdateNotificationRequest request);
}

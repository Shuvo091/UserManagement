using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Modules.Users.Application.Interfaces;

public interface IWorkflowEngineClient
{
	Task<EloUpdateNotificationResponse?> NotifyEloUpdatedAsync(EloUpdateNotificationRequest request);
}

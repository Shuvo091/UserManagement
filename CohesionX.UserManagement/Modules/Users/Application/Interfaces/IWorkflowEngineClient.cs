using CohesionX.UserManagement.Modules.Users.Application.DTOs;

namespace CohesionX.UserManagement.Modules.Users.Application.Interfaces;

public interface IWorkflowEngineClient
{
	Task<EloUpdateNotificationResponse?> NotifyEloUpdatedAsync(EloUpdateNotificationRequest request);
}

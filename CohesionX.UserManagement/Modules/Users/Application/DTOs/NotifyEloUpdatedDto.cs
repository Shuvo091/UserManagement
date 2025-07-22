using CohesionX.UserManagement.Modules.Users.Application.Constants;

namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class EloUpdateNotificationRequest
{
	public string EventType { get; set; } = WorkflowEventType.ELO_UPDATED;
	public string UpdateId { get; set; } = default!;
	public EloUpdateEventData EventData { get; set; } = new EloUpdateEventData();
}

public class EloUpdateEventData
{
	public Guid ComparisonId { get; set; }
	public int UsersUpdated { get; set; }
	public List<EloUpdateResult> UpdateResults { get; set; } = new List<EloUpdateResult>();
}

public class EloUpdateResult
{
	public Guid UserId { get; set; }
	public int NewElo { get; set; }
	public int Change { get; set; }
}

public class EloUpdateNotificationResponse
{
	public bool Acknowledged { get; set; }
	public string WorkflowAction { get; set; } = default!;
}

namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class ThreeWayEloUpdateRequest
{
	public Guid OriginalComparisonId { get; set; }
	public Guid TiebreakerComparisonId { get; set; }
	public string WorkflowRequestId { get; set; } = default!;
	public List<ThreeWayEloChange> ThreeWayEloChanges { get; set; } = [];
	public TiebreakerBonus TiebreakerBonus { get; set; } = new();
}

public class ThreeWayEloChange
{
	public Guid TranscriberId { get; set; }
	public Guid? OpponentId2 { get; set; }
	public Guid OppenentId { get; set; }
	public string Role { get; set; } = default!;
	public string Outcome { get; set; } = default!; // e.g. win, loss, draw
	public int EloChange { get; set; } 
	public int NewElo { get; set; }
}

public class TiebreakerBonus
{
	public bool Applied { get; set; }
	public int BonusAmount { get; set; }
	public string Reason { get; set; } = default!;
}


public class ThreeWayEloUpdateResponse
{
	public bool EloUpdateConfirmed { get; set; }
	public int UpdatesApplied { get; set; }
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	public List<UserNotification> UserNotifications { get; set; } = [];
}

public class UserNotification
{
	public Guid UserId { get; set; }
	public string NotificationType { get; set; } = default!;
	public string Message { get; set; } = default!;
}

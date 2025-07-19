namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class EloUpdateRequest
{
	public string? WorkflowRequestId { get; set; }
	public Guid QaComparisonId { get; set; }
	public string? QaServiceReference { get; set; }
	public List<RecommendedEloChangeDto> RecommendedEloChanges { get; set; } = new();
	public ComparisonMetadataDto ComparisonMetadata { get; set; } = new();
}

public class RecommendedEloChangeDto
{
	public Guid TranscriberId { get; set; }
	public int OldElo { get; set; }
	public int RecommendedChange { get; set; }
	public string? ComparisonOutcome { get; set; }
	public int OpponentElo { get; set; }
}

public class ComparisonMetadataDto
{
	public string? AudioSegmentId { get; set; }
	public string? ComparisonType { get; set; }
	public double QaConfidence { get; set; }
	public string? QaMethod { get; set; }
	public DateTime ComparisonTimestamp { get; set; }
}


public class EloUpdateResponse
{
	public string? WorkflowRequestId { get; set; }
	public List<EloUpdateAppliedDto> EloUpdatesApplied { get; set; } = new();
	public Guid ComparisonId { get; set; }
	public DateTime UpdatedAt { get; set; }
}

public class EloUpdateAppliedDto
{
	public Guid TranscriberId { get; set; }
	public int OldElo { get; set; }
	public int NewElo { get; set; }
	public int EloChange { get; set; }
	public string? ComparisonOutcome { get; set; }
}

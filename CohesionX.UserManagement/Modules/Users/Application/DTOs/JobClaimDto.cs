namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class ClaimJobRequest
{
	public string JobId { get; set; } = default!;
	public DateTime ClaimTimestamp { get; set; }
	public string WorkflowRequestId { get; set; } = default!;
	public JobMetadataDto JobMetadata { get; set; } = default!;
}

public class JobMetadataDto
{
	public string Dialect { get; set; } = default!;
	public string EstimatedDuration { get; set; } = default!; // e.g. "4m30s"
	public int TranscriptionSequence { get; set; }
}

public class ClaimJobResponse
{
	public bool ClaimValidated { get; set; }
	public bool UserEligible { get; set; }
	public Guid ClaimId { get; set; } = default!;
	public string UserAvailability { get; set; } = default!;
	//public bool IsProfessional { get; set; } // To get this info we have to hit database. That is not wanted here
	//public bool BypassQARequired { get; set; }
	public int CurrentWorkload { get; set; }
	public int MaxConcurrentJobs { get; set; }
	public DateTime CapacityReservedUntil { get; set; }
}

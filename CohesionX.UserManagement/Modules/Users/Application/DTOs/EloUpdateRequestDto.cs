namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class EloUpdateRequestDto
{
	public List<EloChangeDto> RecommendedEloChanges { get; set; } = [];
	public ComparisonMetadataDto ComparisonMetadata { get; set; } = default!;
}


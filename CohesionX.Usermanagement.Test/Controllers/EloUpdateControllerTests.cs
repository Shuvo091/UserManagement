// xUnit test setup for EloUpdateController
using Moq;
using Microsoft.AspNetCore.Mvc;
using CohesionX.UserManagement.Controllers;
using CohesionX.UserManagement.Application.Interfaces;
using SharedLibrary.RequestResponseModels.UserManagement;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;

public class EloUpdateControllerTests
{
	private readonly Mock<IEloService> _eloServiceMock = new();
	private readonly Mock<ILogger<EloUpdateController>> _logger = new();
	private readonly EloUpdateController _eloController;

	public EloUpdateControllerTests()
	{
		_eloController = new EloUpdateController(_eloServiceMock.Object, _logger.Object);
	}

	[Fact]
	public async Task EloUpdate_ReturnsOk_WhenValid()
	{
		var request = new EloUpdateRequest
		{
			WorkflowRequestId = "workflow_123",
			QaComparisonId = Guid.NewGuid(),
			RecommendedEloChanges = new List<RecommendedEloChangeDto>
			{
				new() { TranscriberId = Guid.NewGuid(), OldElo = 1200, OpponentElo = 1300, RecommendedChange = 25, ComparisonOutcome = "win" },
				new() { TranscriberId = Guid.NewGuid(), OldElo = 1300, OpponentElo = 1200, RecommendedChange = -25, ComparisonOutcome = "loss" }
			},
			ComparisonMetadata = new ComparisonMetadataDto { QaMethod = "manual", ComparisonType = "binary" }
		};

		_eloServiceMock.Setup(x => x.ApplyEloUpdatesAsync(request)).ReturnsAsync(new EloUpdateResponse
		{
			WorkflowRequestId = request.WorkflowRequestId,
			ComparisonId = request.QaComparisonId,
			UpdatedAt = DateTime.UtcNow,
			EloUpdatesApplied = request.RecommendedEloChanges.Select(c => new EloUpdateAppliedDto
			{
				TranscriberId = c.TranscriberId,
				OldElo = c.OldElo,
				NewElo = c.OldElo + c.RecommendedChange,
				EloChange = c.RecommendedChange,
				ComparisonOutcome = c.ComparisonOutcome
			}).ToList()
		});

		var result = await _eloController.EloUpdate(request);
		var ok = Assert.IsType<OkObjectResult>(result);
		var value = Assert.IsType<EloUpdateResponse>(ok.Value);
		Assert.Equal(request.WorkflowRequestId, value.WorkflowRequestId);
	}

	[Fact]
	public async Task EloUpdate_Returns500_OnException()
	{
		var request = new EloUpdateRequest
		{
			WorkflowRequestId = "workflow_123",
			QaComparisonId = Guid.NewGuid(),
			RecommendedEloChanges = [],
			ComparisonMetadata = new ComparisonMetadataDto()
		};

		_eloServiceMock.Setup(x => x.ApplyEloUpdatesAsync(It.IsAny<EloUpdateRequest>())).ThrowsAsync(new Exception("Unexpected error"));

		var result = await _eloController.EloUpdate(request);

		var status = Assert.IsType<ObjectResult>(result);
		Assert.Equal(500, status.StatusCode);
		Assert.Contains("error", status.Value.ToString());
	}

	[Fact]
	public async Task ThreeWayResolution_ReturnsOk_WhenValid()
	{
		var user1 = Guid.NewGuid();
		var user2 = Guid.NewGuid();
		var tiebreaker = Guid.NewGuid();
		var request = new ThreeWayEloUpdateRequest
		{
			WorkflowRequestId = "wf_001",
			OriginalComparisonId = Guid.NewGuid(),
			ThreeWayEloChanges = new List<ThreeWayEloChange>
			{
				new() { TranscriberId = user1, Role = "OriginalTranscriber1", Outcome = "win", EloChange = 10 },
				new() { TranscriberId = user2, Role = "OriginalTranscriber2", Outcome = "loss", EloChange = -10 },
				new() { TranscriberId = tiebreaker, Role = "TiebreakerTranscriber", Outcome = "draw", EloChange = 0 }
			}
		};

		_eloServiceMock.Setup(x => x.ResolveThreeWay(request)).ReturnsAsync(new ThreeWayEloUpdateResponse
		{
			EloUpdateConfirmed = true,
			UpdatesApplied = 3,
			Timestamp = DateTime.UtcNow,
			UserNotifications = new List<UserNotification>()
		});

		var result = await _eloController.ThreeWayResolution(request);
		var ok = Assert.IsType<OkObjectResult>(result);
		var resp = Assert.IsType<ThreeWayEloUpdateResponse>(ok.Value);
		Assert.True(resp.EloUpdateConfirmed);
		Assert.Equal(3, resp.UpdatesApplied);
	}

	[Fact]
	public async Task ThreeWayResolution_Returns500_OnException()
	{
		var request = new ThreeWayEloUpdateRequest
		{
			WorkflowRequestId = "wf_001",
			OriginalComparisonId = Guid.NewGuid(),
			ThreeWayEloChanges = []
		};

		_eloServiceMock.Setup(x => x.ResolveThreeWay(It.IsAny<ThreeWayEloUpdateRequest>())).ThrowsAsync(new Exception("Failure"));

		var result = await _eloController.ThreeWayResolution(request);

		var status = Assert.IsType<ObjectResult>(result);
		Assert.Equal(500, status.StatusCode);
		Assert.Contains("error", status.Value.ToString());
	}

	[Fact]
	public async Task EloUpdate_ReturnsBadRequest_WhenEloChangeListInvalid()
	{
		var request = new EloUpdateRequest
		{
			WorkflowRequestId = "workflow_123",
			QaComparisonId = Guid.NewGuid(),
			RecommendedEloChanges = null, // or include invalid entries
			ComparisonMetadata = new ComparisonMetadataDto()
		};

		_eloServiceMock.Setup(x => x.ApplyEloUpdatesAsync(It.IsAny<EloUpdateRequest>()))
			.ThrowsAsync(new ArgumentException("RecommendedEloChanges is required"));

		var result = await _eloController.EloUpdate(request);

		var bad = Assert.IsType<BadRequestObjectResult>(result);
		Assert.Contains("RecommendedEloChanges is required", bad.Value.ToString());
	}


	[Fact]
	public async Task ThreeWayResolution_ReturnsBadRequest_WhenWorkflowIdMissing()
	{
		var request = new ThreeWayEloUpdateRequest
		{
			WorkflowRequestId = null,
			OriginalComparisonId = Guid.NewGuid(),
			ThreeWayEloChanges = new()
		};

		_eloServiceMock.Setup(x => x.ResolveThreeWay(It.IsAny<ThreeWayEloUpdateRequest>()))
			.ThrowsAsync(new ArgumentException("WorkflowRequestId is required"));

		var result = await _eloController.ThreeWayResolution(request);

		var bad = Assert.IsType<BadRequestObjectResult>(result);
		Assert.Contains("WorkflowRequestId is required", bad.Value.ToString());
	}

}

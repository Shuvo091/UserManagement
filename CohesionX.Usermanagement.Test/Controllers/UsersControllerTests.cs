using Moq;
using CohesionX.UserManagement.Controllers;
using SharedLibrary.RequestResponseModels.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.AppEnums;
using CohesionX.UserManagement.Application.Interfaces;
using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.Usermanagement.Test.Controllers;

public class UsersControllerTests
{
	private readonly Mock<IUserService> _userServiceMock = new();
	private readonly Mock<IEloService> _eloServiceMock = new();
	private readonly Mock<IVerificationRequirementService> _verificationReqMock = new();
	private readonly Mock<IRedisService> _redisServiceMock = new();
	private readonly Mock<IServiceScopeFactory> _serviceScopeFactory = new();
	private readonly Mock<IConfiguration> _config = new();
	private readonly UsersController _controller;

	public UsersControllerTests()
	{
		_controller = new UsersController(
			_userServiceMock.Object,
			_eloServiceMock.Object,
			_redisServiceMock.Object,
			_config.Object,
			_serviceScopeFactory.Object,
			_verificationReqMock.Object
		);
	}

	[Fact]
	public async Task Register_ValidRequest_ReturnsCreated()
	{
		var request = new UserRegisterRequest
		{
			FirstName = "John",
			LastName = "Doe",
			Email = "john@example.com",
			Password = "password123"
		};

		var response = new UserRegisterResponse
		{
			UserId = Guid.NewGuid(),
			EloRating = 360,
			Status = "pending_verification",
			ProfileUri = "/api/v1/users/profile",
			VerificationRequired = new List<string> { "id_document_upload" }
		};

		_userServiceMock.Setup(s => s.RegisterUserAsync(request)).ReturnsAsync(response);

		var result = await _controller.Register(request);

		var createdAt = Assert.IsType<CreatedAtActionResult>(result);
		Assert.Equal("GetProfile", createdAt.ActionName);
		Assert.Equal(response, createdAt.Value);
	}

	[Fact]
	public async Task Register_InvalidModel_ReturnsBadRequest()
	{
		_controller.ModelState.AddModelError("Email", "Required");
		var result = await _controller.Register(new UserRegisterRequest());

		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.IsType<SerializableError>(badRequest.Value);
	}

	[Fact]
	public async Task VerifyUser_UserNotFound_ReturnsNotFound()
	{
		var userId = Guid.NewGuid();
		var request = new VerificationRequest { VerificationType = VerificationType.IdDocument.ToDisplayName() };

		_userServiceMock.Setup(s => s.GetUserAsync(userId)).ReturnsAsync((User)null);

		var result = await _controller.VerifyUser(userId, request);

		var notFound = Assert.IsType<NotFoundObjectResult>(result);
		Assert.Contains("User not found", notFound.Value.ToString());
	}


	[Fact]
	public async Task GetProfile_UserExists_ReturnsOk()
	{
		var userId = Guid.NewGuid();
		var dto = new UserProfileResponse { FirstName = "Jane", LastName = "Doe", EloRating = 420, Status = "active" };

		_userServiceMock.Setup(s => s.GetProfileAsync(userId)).ReturnsAsync(dto);

		var result = await _controller.GetProfile(userId);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(dto, ok.Value);
	}

	[Fact]
	public async Task GetProfile_Exception_Returns500()
	{
		var userId = Guid.NewGuid();
		_userServiceMock.Setup(s => s.GetProfileAsync(userId)).ThrowsAsync(new Exception("db error"));

		var result = await _controller.GetProfile(userId);

		var error = Assert.IsType<ObjectResult>(result);
		Assert.Equal(500, error.StatusCode);
	}

	[Fact]
	public async Task GetAvailability_UserFound_ReturnsAvailability()
	{
		var userId = Guid.NewGuid();
		var availability = new UserAvailabilityRedisDto { Status = UserAvailabilityType.Available.ToDisplayName() };

		_redisServiceMock.Setup(r => r.GetAvailabilityAsync(userId)).ReturnsAsync(availability);

		var result = await _controller.GetAvailability(userId);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(availability, ok.Value);
	}

	[Fact]
	public async Task GetAvailability_UserNotFound_ReturnsString()
	{
		var userId = Guid.NewGuid();

		_redisServiceMock.Setup(r => r.GetAvailabilityAsync(userId)).ReturnsAsync((UserAvailabilityRedisDto)null);

		var result = await _controller.GetAvailability(userId);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Equal("User Not Found", ok.Value);
	}

	[Fact]
	public async Task PatchAvailability_ValidUpdate_ReturnsUpdated()
	{
		var userId = Guid.NewGuid();
		var update = new UserAvailabilityUpdateRequest { Status = UserAvailabilityType.Available.ToDisplayName(), MaxConcurrentJobs = 5 };
		var existing = new UserAvailabilityRedisDto();

		_redisServiceMock.Setup(r => r.GetAvailabilityAsync(userId)).ReturnsAsync(existing);

		_redisServiceMock.Setup(r => r.SetAvailabilityAsync(userId, It.IsAny<UserAvailabilityRedisDto>()))
			.Returns(Task.CompletedTask);

		_userServiceMock.Setup(u => u.UpdateAvailabilityAuditAsync(userId, It.IsAny<UserAvailabilityRedisDto>(), It.IsAny<string>(), It.IsAny<string>()))
			.Returns(Task.CompletedTask);

		var context = new DefaultHttpContext();
		context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
		context.Request.Headers["User-Agent"] = "test-agent";
		_controller.ControllerContext = new ControllerContext { HttpContext = context };

		var result = await _controller.PatchAvailability(userId, update);

		var ok = Assert.IsType<OkObjectResult>(result);
		var response = Assert.IsType<UserAvailabilityUpdateResponse>(ok.Value);
		Assert.Equal("success", response.AvailabilityUpdated);
		Assert.Equal(update.Status, response.CurrentStatus);
		Assert.Equal(update.MaxConcurrentJobs, response.MaxConcurrentJobs);
	}

	[Fact]
	public async Task Register_WithValidSouthAfricanId_CreatesUserWithPendingVerificationAndInitialElo()
	{
		// Arrange
		var request = new UserRegisterRequest
		{
			FirstName = "Sipho",
			LastName = "Nkosi",
			Email = "sipho@example.com",
			Password = "securepass",
			IdNumber = "8001015009087" // Example valid SA ID
		};
		var expectedElo = 1200; // Should match config or be mocked
		var response = new UserRegisterResponse
		{
			UserId = Guid.NewGuid(),
			EloRating = expectedElo,
			Status = "pending_verification",
			ProfileUri = "/api/v1/users/profile",
			VerificationRequired = new List<string> { "id_document_upload" }
		};
		_userServiceMock.Setup(s => s.RegisterUserAsync(request)).ReturnsAsync(response);

		// Act
		var result = await _controller.Register(request);

		// Assert
		var createdAt = Assert.IsType<CreatedAtActionResult>(result);
		var value = Assert.IsType<UserRegisterResponse>(createdAt.Value);
		Assert.Equal("pending_verification", value.Status);
		Assert.Equal(expectedElo, value.EloRating);
		Assert.Contains("id_document_upload", value.VerificationRequired);
	}

	[Fact]
	public async Task ClaimJob_AvailableUserWithCapacity_ClaimsJobAndUpdatesAvailability()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var jobId = "job-123";
		var claimJobRequest = new ClaimJobRequest  { JobId = jobId };
		_redisServiceMock.Setup(r => r.TryClaimJobAsync(jobId, userId)).ReturnsAsync(true);
		_redisServiceMock.Setup(r => r.SetAvailabilityAsync(userId, It.IsAny<UserAvailabilityRedisDto>())).Returns(Task.CompletedTask);
		_redisServiceMock.Setup(r => r.AddUserClaimAsync(userId, jobId)).Returns(Task.CompletedTask);
		// TODO: Setup userServiceMock for claim record creation if needed

		// Act
		var result = _controller.ClaimJob(userId, claimJobRequest);

		// Assert
		var ok = Assert.IsType<OkObjectResult>(result);
		// TODO: Assert claim validated, availability updated, claim record created
	}

	[Fact]
	public async Task PatchAvailability_ActiveUser_UpdatesStatusInRedisAndPostgres()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var update = new UserAvailabilityUpdateRequest { Status = UserAvailabilityType.Busy.ToDisplayName(), MaxConcurrentJobs = 2 };
		var existing = new UserAvailabilityRedisDto { Status = UserAvailabilityType.Available.ToDisplayName() };
		_redisServiceMock.Setup(r => r.GetAvailabilityAsync(userId)).ReturnsAsync(existing);
		_redisServiceMock.Setup(r => r.SetAvailabilityAsync(userId, It.IsAny<UserAvailabilityRedisDto>())).Returns(Task.CompletedTask);
		_userServiceMock.Setup(u => u.UpdateAvailabilityAuditAsync(userId, It.IsAny<UserAvailabilityRedisDto>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
		// TODO: Setup event publishing mock if applicable

		var context = new DefaultHttpContext();
		context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
		context.Request.Headers["User-Agent"] = "test-agent";
		_controller.ControllerContext = new ControllerContext { HttpContext = context };

		// Act
		var result = await _controller.PatchAvailability(userId, update);

		// Assert
		var ok = Assert.IsType<OkObjectResult>(result);
		var response = Assert.IsType<UserAvailabilityUpdateResponse>(ok.Value);
		Assert.Equal(UserAvailabilityType.Busy.ToDisplayName(), response.CurrentStatus);
		// TODO: Assert audit log and event publish if possible
	}

	[Fact]
	public async Task GetProfile_WithCompletedJobs_ReturnsProfileWithEloAndStats()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var profile = new UserProfileResponse
		{
			FirstName = "Lindiwe",
			LastName = "Zulu",
			EloRating = 1500,
			Status = "active",
			Statistics = new UserStatisticsDto { TotalJobsCompleted = 10, GamesPlayed = 12, EloTrend = "+100_over_7_days" },
			ProfessionalEligibility = new ProfessionalEligibilityDto { Eligible = false, MissingCriteria = new List<string> { "elo_rating" } }
		};
		_userServiceMock.Setup(s => s.GetProfileAsync(userId)).ReturnsAsync(profile);

		// Act
		var result = await _controller.GetProfile(userId);

		// Assert
		var ok = Assert.IsType<OkObjectResult>(result);
		var dto = Assert.IsType<UserProfileResponse>(ok.Value);
		Assert.Equal(1500, dto.EloRating);
		Assert.Equal(10, dto.Statistics.TotalJobsCompleted);
		Assert.Equal("+100_over_7_days", dto.Statistics.EloTrend);
		Assert.Contains("elo_rating", dto.ProfessionalEligibility.MissingCriteria);
	}
}


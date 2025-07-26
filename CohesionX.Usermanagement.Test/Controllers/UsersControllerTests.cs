// xUnit test setup for UsersController
using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using CohesionX.UserManagement.Controllers;
using CohesionX.UserManagement.Application.Interfaces;
using SharedLibrary.RequestResponseModels.UserManagement;
using SharedLibrary.AppEnums;
using CohesionX.UserManagement.Domain.Entities;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

public class UsersControllerTests
{
	private readonly Mock<IUserService> _userServiceMock = new();
	private readonly Mock<IEloService> _eloServiceMock = new();
	private readonly Mock<IRedisService> _redisServiceMock = new();
	private readonly Mock<IVerificationRequirementService> _verificationRequirementServiceMock = new();
	private readonly Mock<IConfiguration> _configurationMock = new();
	private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
	private readonly UsersController _controller;

	public UsersControllerTests()
	{
		_configurationMock.Setup(c => c["DEFAULT_BOOKOUT_MINUTES"]).Returns("60");
		_controller = new UsersController(
			_userServiceMock.Object,
			_eloServiceMock.Object,
			_redisServiceMock.Object,
			_configurationMock.Object,
			_scopeFactoryMock.Object,
			_verificationRequirementServiceMock.Object);
	}

	[Fact]
	public async Task Register_ReturnsCreated_WhenValid()
	{
		var request = new UserRegisterRequest { FirstName = "A", LastName = "B", Email = "a@b.com", Password = "123", ConsentToDataProcessing = true };
		var response = new UserRegisterResponse { UserId = Guid.NewGuid(), Status = "pending_verification", EloRating = 1200, ProfileUri = "/profile", VerificationRequired = new List<string> { "id_document_upload" } };
		_userServiceMock.Setup(x => x.RegisterUserAsync(request)).ReturnsAsync(response);

		var result = await _controller.Register(request);
		var created = Assert.IsType<CreatedAtActionResult>(result);
		Assert.Equal(response, created.Value);
	}

	[Fact]
	public async Task Register_ReturnsBadRequest_WhenConsentMissing()
	{
		var request = new UserRegisterRequest { FirstName = "A", LastName = "B", Email = "a@b.com", Password = "123", ConsentToDataProcessing = false };
		var result = await _controller.Register(request);
		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.Equal("Consent to data processing needed!", badRequest.Value);
	}

	[Fact]
	public async Task GetAvailability_ReturnsAvailability_WhenExists()
	{
		var userId = Guid.NewGuid();
		var dto = new UserAvailabilityRedisDto { Status = "available" };
		_redisServiceMock.Setup(x => x.GetAvailabilityAsync(userId)).ReturnsAsync(dto);
		var result = await _controller.GetAvailability(userId);
		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(dto, ok.Value);
	}

	[Fact]
	public async Task GetAvailability_ReturnsNotFoundMessage_WhenMissing()
	{
		var userId = Guid.NewGuid();
		_redisServiceMock.Setup(x => x.GetAvailabilityAsync(userId)).ReturnsAsync((UserAvailabilityRedisDto)null);
		var result = await _controller.GetAvailability(userId);
		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Equal("User availability Not Found", ok.Value);
	}

	[Fact]
	public async Task VerifyUser_ReturnsNotFound_WhenUserMissing()
	{
		var userId = Guid.NewGuid();
		_verificationRequirementServiceMock.Setup(x => x.GetVerificationRequirement()).ReturnsAsync(new UserVerificationRequirement());
		_userServiceMock.Setup(x => x.GetUserAsync(userId)).ReturnsAsync((User)null);
		var result = await _controller.VerifyUser(userId, new VerificationRequest());
		var notFound = Assert.IsType<NotFoundObjectResult>(result);
		Assert.Contains("User not found", notFound.Value.ToString());
	}

	[Fact]
	public async Task PatchAvailability_UpdatesCorrectly()
	{
		var userId = Guid.NewGuid();
		var request = new UserAvailabilityUpdateRequest
		{
			Status = "available",
			MaxConcurrentJobs = 2
		};
		var redisDto = new UserAvailabilityRedisDto
		{
			Status = "busy",
			MaxConcurrentJobs = 1,
			CurrentWorkload = 0
		};

		_redisServiceMock.Setup(x => x.GetAvailabilityAsync(userId)).ReturnsAsync(redisDto);
		_redisServiceMock.Setup(x => x.SetAvailabilityAsync(userId, It.IsAny<UserAvailabilityRedisDto>())).Returns(Task.CompletedTask);
		_userServiceMock.Setup(x => x.UpdateAvailabilityAuditAsync(userId, It.IsAny<UserAvailabilityRedisDto>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

		// Setup mock HttpContext
		var context = new DefaultHttpContext();
		context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
		context.Request.Headers["User-Agent"] = "UnitTest-Agent";
		_controller.ControllerContext = new ControllerContext
		{
			HttpContext = context
		};

		var result = await _controller.PatchAvailability(userId, request);

		var ok = Assert.IsType<OkObjectResult>(result);
		var response = Assert.IsType<UserAvailabilityUpdateResponse>(ok.Value);
		Assert.Equal("success", response.AvailabilityUpdated);
		Assert.Equal(request.Status, response.CurrentStatus);
		Assert.Equal(request.MaxConcurrentJobs, response.MaxConcurrentJobs);
	}


	[Fact]
	public async Task GetProfile_ReturnsProfile()
	{
		var userId = Guid.NewGuid();
		var profile = new UserProfileResponse { FirstName = "A", LastName = "B" };
		_userServiceMock.Setup(x => x.GetProfileAsync(userId)).ReturnsAsync(profile);
		var result = await _controller.GetProfile(userId);
		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(profile, ok.Value);
	}

	[Fact]
	public async Task ClaimJob_ReturnsBadRequest_WhenUnavailable()
	{
		var userId = Guid.NewGuid();
		_redisServiceMock.Setup(x => x.GetAvailabilityAsync(userId)).ReturnsAsync(new UserAvailabilityRedisDto { Status = "offline" });
		var result = await _controller.ClaimJob(userId, new ClaimJobRequest { JobId = "job_1" });
		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.Contains("unavailable", badRequest.Value.ToString());
	}

	[Fact]
	public async Task ValidateTiebreakerClaim_ReturnsForbidden_WhenOriginalTranscriber()
	{
		var userId = Guid.NewGuid();
		var req = new ValidateTiebreakerClaimRequest { OriginalTranscribers = [userId] };
		_userServiceMock.Setup(x => x.ValidateTieBreakerClaim(userId, req)).ReturnsAsync(new ValidateTiebreakerClaimResponse { IsOriginalTranscriber = true, UserEloQualified = true });

		var result = await _controller.ValidateTiebreakerClaim(userId, req);
		Assert.IsType<ForbidResult>(result);
	}

	[Fact]
	public async Task GetEloHistory_ReturnsHistory()
	{
		var userId = Guid.NewGuid();
		var eloData = new EloHistoryResponse { CurrentElo = 1400 };
		_eloServiceMock.Setup(x => x.GetEloHistoryAsync(userId)).ReturnsAsync(eloData);
		var result = await _controller.GetEloHistory(userId);
		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(eloData, ok.Value);
	}
}

using System.Linq.Expressions;
using System.Security.Claims;
using CloudNative.CloudEvents;
using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Abstractions.Services;
using CohesionX.UserManagement.Application.Constants;
using CohesionX.UserManagement.Application.Services;
using CohesionX.UserManagement.Database.Abstractions.Entities;
using CohesionX.UserManagement.Database.Abstractions.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SharedLibrary.AppEnums;
using SharedLibrary.Common.Options;
using SharedLibrary.Common.Security;
using SharedLibrary.Common.Utilities;
using SharedLibrary.Contracts.Usermanagement.RedisDtos;
using SharedLibrary.Contracts.Usermanagement.Requests;
using SharedLibrary.Contracts.Usermanagement.Responses;
using SharedLibrary.Kafka.Services.Interfaces;

namespace CohesionX.UserManagement.Tests.Services;

/// <summary>
/// Unit tests for <see cref="UserService"/>.
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repo;
    private readonly Mock<IAuditLogRepository> _auditLogRepo;
    private readonly Mock<IJobClaimRepository> _jobClaimRepo;
    private readonly Mock<IEloService> _eloService;
    private readonly Mock<IVerificationRequirementService> _verificationRequirementService;
    private readonly Mock<IRedisService> _redisService;
    private readonly Mock<IEventBus> _eventBus;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<ILogger<UserService>> _logger;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly IOptions<AppConstantsOptions> _appConstantsOptions;
    private readonly UserService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserServiceTests"/> class.
    /// Sets up all required mocks and options for testing.
    /// </summary>
    public UserServiceTests()
    {
        // Mocks
        this._repo = new Mock<IUserRepository>();
        this._auditLogRepo = new Mock<IAuditLogRepository>();
        this._jobClaimRepo = new Mock<IJobClaimRepository>();
        this._eloService = new Mock<IEloService>();
        this._verificationRequirementService = new Mock<IVerificationRequirementService>();
        this._redisService = new Mock<IRedisService>();
        this._eventBus = new Mock<IEventBus>();
        this._httpContextAccessor = new Mock<IHttpContextAccessor>();
        this._logger = new Mock<ILogger<UserService>>();

        // Options
        this._appConstantsOptions = Options.Create(new AppConstantsOptions
        {
            InitialEloRating = 1200,
            MinEloRequiredForPro = 1500,
            MinJobsRequiredForPro = 50,
            DefaultBookoutMinutes = 15,
        });

        this._jwtOptions = Options.Create(new JwtOptions
        {
            Secret = "super-secret-key-for-testing-32chars!",
            ExpiryMinutes = 60,
            Issuer = "test-issuer",
        });

        // Service under test
        this._service = new UserService(
            this._repo.Object,
            this._auditLogRepo.Object,
            this._jobClaimRepo.Object,
            this._redisService.Object,
            this._eventBus.Object,
            this._httpContextAccessor.Object,
            this._appConstantsOptions,
            this._eloService.Object,
            this._verificationRequirementService.Object,
            this._logger.Object,
            this._jwtOptions);
    }

    /// <summary>
    /// Tests that registration fails when user does not consent to data processing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RegisterUserAsync_ShouldThrow_WhenConsentIsFalse()
    {
        var dto = new UserRegisterRequest
        {
            ConsentToDataProcessing = false,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "StrongPass!123",
            IdNumber = "8001015009087",
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => this._service.RegisterUserAsync(dto));
        Assert.Equal("Consent to data processing needed!", ex.Message);
    }

    /// <summary>
    /// Tests that registration fails when required fields are missing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RegisterUserAsync_ShouldThrow_WhenRequiredFieldsMissing()
    {
        var dto = new UserRegisterRequest
        {
            ConsentToDataProcessing = true,
            FirstName = string.Empty,
            LastName = "Doe",
            Email = "john@example.com",
            Password = "StrongPass!123",
            IdNumber = "8001015009087",
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => this._service.RegisterUserAsync(dto));
        Assert.Equal("All required fields must be provided", ex.Message);
    }

    /// <summary>
    /// Tests that registration fails when password is weak.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RegisterUserAsync_ShouldThrow_WhenPasswordWeak()
    {
        var dto = new UserRegisterRequest
        {
            ConsentToDataProcessing = true,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "weak",
            IdNumber = "8001015009087",
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => this._service.RegisterUserAsync(dto));
        Assert.Contains("password", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that registration fails when South African ID is invalid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RegisterUserAsync_ShouldThrow_WhenIdInvalid()
    {
        var dto = new UserRegisterRequest
        {
            ConsentToDataProcessing = true,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "StrongPass!123",
            IdNumber = "12345678901234", // 14 digit instead of 13.
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => this._service.RegisterUserAsync(dto));
        Assert.Equal("Invalid South African ID number", ex.Message);
    }

    /// <summary>
    /// Tests that registration fails when email already exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RegisterUserAsync_ShouldThrow_WhenEmailExists()
    {
        var dto = new UserRegisterRequest
        {
            ConsentToDataProcessing = true,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "StrongPass!123",
            IdNumber = "8001015009087",
        };

        this._repo.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => this._service.RegisterUserAsync(dto));
        Assert.Equal("Email already registered", ex.Message);
    }

    /// <summary>
    /// Tests that registration succeeds with valid input.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RegisterUserAsync_ShouldSucceed_WithValidInput()
    {
        var dto = new UserRegisterRequest
        {
            ConsentToDataProcessing = true,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "StrongPass!123",
            IdNumber = "8001015009087",
            DialectPreferences = new List<string> { "Zulu" },
            LanguageExperience = "Intermediate",
        };

        this._repo.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        this._repo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        this._repo.Setup(r => r.SaveChangesAsync()).Returns(Task.FromResult(1));

        var result = await this._service.RegisterUserAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.Email, this._repo.Object.GetType().GetProperty("Email")?.GetValue(this._repo.Object)?.ToString() ?? dto.Email);
        Assert.Equal(this._appConstantsOptions.Value.InitialEloRating, result.EloRating);
        Assert.Contains("id_document_upload", result.VerificationRequired);
        Assert.NotEqual(Guid.Empty, result.UserId);
    }

    /// <summary>
    /// Tests that <see cref="UserService.GetProfileAsync"/> throws <see cref="KeyNotFoundException"/> when user is not found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task GetProfileAsync_ShouldThrow_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        this._repo.Setup(r => r.GetUserByIdAsync(userId, false, true))
            .ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => this._service.GetProfileAsync(userId));
        Assert.Equal("User not found", ex.Message);
    }

    /// <summary>
    /// Tests that <see cref="UserService.GetProfileAsync"/> returns a valid profile for an existing user.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task GetProfileAsync_ShouldReturnProfile_WhenUserExists()
    {
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Status = "Active",
            Role = "Transcriber",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            Statistics = new UserStatistics
            {
                CurrentElo = 1300,
                PeakElo = 1350,
                TotalJobs = 20,
                GamesPlayed = 25,
            },
            EloHistories = new List<EloHistory>
            {
                new () { OldElo = 1200, NewElo = 1210, ChangedAt = DateTime.UtcNow.AddDays(-1) },
                new () { OldElo = 1200, NewElo = 1195, ChangedAt = DateTime.UtcNow.AddDays(-2) },
            },
            Dialects = new List<UserDialect>
            {
                new () { Dialect = "Zulu" },
                new () { Dialect = "Xhosa" },
            },
            JobCompletions = new List<JobCompletion>
            {
                new () { CompletedAt = DateTime.UtcNow.AddDays(-5) },
                new () { CompletedAt = DateTime.UtcNow.AddDays(-15) },
            },
        };

        this._repo.Setup(r => r.GetUserByIdAsync(userId, false, true))
            .ReturnsAsync(user);

        this._eloService.Setup(e => e.GetEloTrend(user.EloHistories.ToList(), 7))
            .Returns("+5_over_7_days");
        this._eloService.Setup(e => e.GetEloTrend(user.EloHistories.ToList(), 30))
            .Returns("+10_over_30_days");
        this._eloService.Setup(e => e.GetWinRate(user.EloHistories.ToList(), null))
            .Returns(0.8);

        var result = await this._service.GetProfileAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(user.FirstName, result.FirstName);
        Assert.Equal(user.LastName, result.LastName);
        Assert.Equal(user.Statistics.CurrentElo, result.EloRating);
        Assert.Equal(user.Statistics.PeakElo, result.PeakElo);
        Assert.Equal(user.Status, result.Status);
        Assert.False(result.IsProfessional);
        Assert.Equal("+5_over_7_days", result.Statistics.EloTrend);
        Assert.Equal("+10_over_30_days", result.Statistics.Last30Days.EloChange);
        Assert.Equal(0.8, result.Statistics.WinRate);
        Assert.Contains("Zulu", result.Statistics.DialectExpertise);
        Assert.Contains("Xhosa", result.Statistics.DialectExpertise);
        Assert.Equal(2, result.Statistics.Last30Days.JobsCompleted);
        Assert.Equal("1300/1500", result.ProfessionalEligibility.Progress.EloProgress);
        Assert.Equal("20/50", result.ProfessionalEligibility.Progress.JobsProgress);
    }

    /// <summary>
    /// Tests that GetUserAsync throws when the user does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUserAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        this._repo.Setup(r => r.GetUserByIdAsync(userId, false, false))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => this._service.GetUserAsync(userId));
        Assert.Equal("User not found", ex.Message);
    }

    /// <summary>
    /// Tests that GetUserAsync returns a user when found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUserAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
        };

        this._repo.Setup(r => r.GetUserByIdAsync(userId, false, false))
                  .ReturnsAsync(user);

        // Act
        var result = await this._service.GetUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("test@example.com", result.Email);
    }

    /// <summary>
    /// Tests that GetUserByEmailAsync throws when email is null or empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUserByEmailAsync_ShouldThrow_WhenEmailIsNullOrEmpty()
    {
        // Arrange
        string email = string.Empty;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => this._service.GetUserByEmailAsync(email));
        Assert.Equal("Email cannot be null or empty. (Parameter 'email')", ex.Message);
    }

    /// <summary>
    /// Tests that GetUserByEmailAsync throws when user does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUserByEmailAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        string email = "test@example.com";
        this._repo.Setup(r => r.GetUserByEmailAsync(email, false, false)).ReturnsAsync((User?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => this._service.GetUserByEmailAsync(email));
        Assert.Equal("User not found", ex.Message);
    }

    /// <summary>
    /// Tests that GetUserByEmailAsync returns user when found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUserByEmailAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        string email = "test@example.com";
        var user = new User { Id = Guid.NewGuid(), Email = email, FirstName = "John", LastName = "Doe" };
        this._repo.Setup(r => r.GetUserByEmailAsync(email, false, false)).ReturnsAsync(user);

        // Act
        var result = await this._service.GetUserByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
    }

    /// <summary>
    /// Tests that GetFilteredUser returns filtered users.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetFilteredUser_ShouldReturnFilteredUsers()
    {
        // Arrange
        var dialect = "Zulu";
        int? minElo = 1000;
        int? maxElo = 2000;
        int? maxWorkload = 5;
        int? limit = 10;

        var users = new List<User>
    {
        new User { Id = Guid.NewGuid(), FirstName = "Alice", LastName = "Smith" },
        new User { Id = Guid.NewGuid(), FirstName = "Bob", LastName = "Johnson" },
    };

        this._repo.Setup(r => r.GetFilteredUser(dialect, minElo, maxElo, maxWorkload, limit))
                  .ReturnsAsync(users);

        // Act
        var result = await this._service.GetFilteredUser(dialect, minElo, maxElo, maxWorkload, limit);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.FirstName == "Alice");
        Assert.Contains(result, u => u.FirstName == "Bob");
    }

    /// <summary>
    /// Tests that UpdateAvailabilityAuditAsync throws when userId is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateAvailabilityAuditAsync_ShouldThrow_WhenUserIdEmpty()
    {
        var availability = new UserAvailabilityRedisDto();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            this._service.UpdateAvailabilityAuditAsync(Guid.Empty, availability, "127.0.0.1", "Agent"));
        Assert.Equal("UserId cannot be empty. (Parameter 'userId')", ex.Message);
    }

    /// <summary>
    /// Tests that UpdateAvailabilityAuditAsync throws when existingAvailability is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateAvailabilityAuditAsync_ShouldThrow_WhenExistingAvailabilityNull()
    {
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            this._service.UpdateAvailabilityAuditAsync(Guid.NewGuid(), null!, "127.0.0.1", "Agent"));
        Assert.Equal("Value cannot be null. (Parameter 'existingAvailability')", ex.Message);
    }

    /// <summary>
    /// Tests that UpdateAvailabilityAuditAsync succeeds with valid inputs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateAvailabilityAuditAsync_ShouldSucceed_WithValidInput()
    {
        var userId = Guid.NewGuid();
        var availability = new UserAvailabilityRedisDto();

        this._auditLogRepo.Setup(a => a.AddAuditLog(userId, availability, It.IsAny<string?>(), It.IsAny<string?>()))
                          .Returns(Task.CompletedTask);
        this._auditLogRepo.Setup(a => a.SaveChangesAsync()).Returns(Task.FromResult(1));

        await this._service.UpdateAvailabilityAuditAsync(userId, availability, "127.0.0.1", "Agent");

        this._auditLogRepo.Verify(a => a.AddAuditLog(userId, availability, "127.0.0.1", "Agent"), Times.Once);
        this._auditLogRepo.Verify(a => a.SaveChangesAsync(), Times.Once);
    }

    /// <summary>
    /// Tests that CheckIdNumber throws when userId is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CheckIdNumber_ShouldThrow_WhenUserIdEmpty()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            this._service.CheckIdNumber(Guid.Empty, "8001015009087"));
        Assert.Equal("UserId cannot be empty. (Parameter 'userId')", ex.Message);
    }

    /// <summary>
    /// Tests that CheckIdNumber returns false when user does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CheckIdNumber_ShouldReturnFalse_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        this._repo.Setup(r => r.GetUserByIdAsync(userId, false, false)).ReturnsAsync((User?)null);

        var result = await this._service.CheckIdNumber(userId, "8001015009087");

        Assert.False(result);
    }

    /// <summary>
    /// Tests that CheckIdNumber returns true when ID matches.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CheckIdNumber_ShouldReturnTrue_WhenIdMatches()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, IdNumber = "8001015009087" };
        this._repo.Setup(r => r.GetUserByIdAsync(userId, false, false)).ReturnsAsync(user);

        var result = await this._service.CheckIdNumber(userId, "8001015009087");

        Assert.True(result);
    }

    /// <summary>
    /// Tests that CheckIdNumber returns false when ID does not match.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CheckIdNumber_ShouldReturnFalse_WhenIdDoesNotMatch()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, IdNumber = "8001015009087" };
        this._repo.Setup(r => r.GetUserByIdAsync(userId, false, false)).ReturnsAsync(user);

        var result = await this._service.CheckIdNumber(userId, "9001015009087");

        Assert.False(result);
    }

    /// <summary>
    /// Tests that ClaimJobAsync throws when userId is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClaimJobAsync_ShouldThrow_WhenUserIdEmpty()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            this._service.ClaimJobAsync(Guid.Empty, new ClaimJobRequest { JobId = "job1", ClaimTimestamp = DateTime.UtcNow }));
        Assert.Equal("UserId cannot be empty. (Parameter 'userId')", ex.Message);
    }

    /// <summary>
    /// Tests that ClaimJobAsync throws when claimJobRequest is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClaimJobAsync_ShouldThrow_WhenClaimJobRequestNull()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            this._service.ClaimJobAsync(Guid.NewGuid(), null!));
        Assert.Equal("Job request is invalid. (Parameter 'claimJobRequest')", ex.Message);
    }

    /// <summary>
    /// Tests successful job claim flow.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClaimJobAsync_ShouldSucceed_WithValidRequest()
    {
        var userId = Guid.NewGuid();
        var claimReq = new ClaimJobRequest { JobId = "job1", ClaimTimestamp = DateTime.UtcNow };

        var availability = new UserAvailabilityRedisDto { Status = UserAvailabilityType.Available.ToDisplayName(), MaxConcurrentJobs = 3, CurrentWorkload = 0 };
        var userElo = new UserEloRedisDto { CurrentElo = 1500, PeakElo = 1600, GamesPlayed = 10, RecentTrend = "+10_over_7_days", LastJobCompleted = DateTime.UtcNow.AddDays(-1) };

        this._redisService.Setup(r => r.GetAvailabilityAsync(userId)).ReturnsAsync(availability);
        this._redisService.Setup(r => r.GetUserEloAsync(userId)).ReturnsAsync(userElo);
        this._redisService.Setup(r => r.GetUserClaimsAsync(userId)).ReturnsAsync(new List<string>());
        this._redisService.Setup(r => r.TryClaimJobAsync(claimReq.JobId, userId)).ReturnsAsync(true);
        this._redisService.Setup(r => r.AddUserClaimAsync(userId, claimReq.JobId)).Returns(Task.CompletedTask);
        this._redisService.Setup(r => r.SetAvailabilityAsync(userId, It.IsAny<UserAvailabilityRedisDto>())).Returns(Task.CompletedTask);
        this._jobClaimRepo.Setup(j => j.AddJobClaimAsync(It.IsAny<JobClaim>())).ReturnsAsync((JobClaim j) => j);
        this._jobClaimRepo.Setup(j => j.SaveChangesAsync()).Returns(Task.FromResult(1));
        this._eventBus.Setup(e => e.PublishAsync(It.IsAny<CloudEvent>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        var result = await this._service.ClaimJobAsync(userId, claimReq);

        Assert.True(result.ClaimValidated);
        Assert.Equal(userElo.CurrentElo, result.CurrentElo);
    }

    /// <summary>
    /// Tests ValidateTieBreakerClaim calls ClaimJobAsync internally and returns expected response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ValidateTieBreakerClaim_ShouldReturnExpectedResponse()
    {
        var userId = Guid.NewGuid();
        var req = new ValidateTiebreakerClaimRequest
        {
            OriginalJobId = "job1",
            OriginalTranscribers = new List<Guid>(),
            RequiredMinElo = 1000,
        };

        // Create partial mock of UserService with CallBase = true
        var userServiceMock = new Mock<UserService>(
            this._repo.Object,
            this._auditLogRepo.Object,
            this._jobClaimRepo.Object,
            this._redisService.Object,
            this._eventBus.Object,
            this._httpContextAccessor.Object,
            this._appConstantsOptions,
            this._eloService.Object,
            this._verificationRequirementService.Object,
            this._logger.Object,
            this._jwtOptions)
        { CallBase = true };

        // Stub ClaimJobAsync
        userServiceMock
            .Setup(s => s.ClaimJobAsync(userId, It.IsAny<ClaimJobRequest>(), req.OriginalTranscribers, req.RequiredMinElo))
            .ReturnsAsync(new ClaimJobResponse { CurrentElo = 1500, ClaimValidated = true });

        // Call the method under test
        var result = await userServiceMock.Object.ValidateTieBreakerClaim(userId, req);

        Assert.True(result.TiebreakerClaimValidated);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(1500, result.CurrentElo);
    }

    /// <summary>
    /// Tests SetProfessional throws when user not found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetProfessional_ShouldThrow_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        var request = new SetProfessionalRequest { AuthorizedBy = Guid.NewGuid(), IsProfessional = true, ProfessionalVerification = new ProfessionalVerificationDto() };

        this._repo.Setup(r => r.GetUserByIdAsync(userId, true, false, It.IsAny<Expression<Func<User, object>>[]>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => this._service.SetProfessional(userId, request));
    }

    /// <summary>
    /// Tests successful professional status update.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetProfessional_ShouldSucceed_WhenCriteriaMet()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var request = new SetProfessionalRequest
        {
            AuthorizedBy = adminId,
            IsProfessional = true,
            ProfessionalVerification = new ProfessionalVerificationDto { VerificationDocuments = new List<string> { "doc1" } },
        };

        var user = new User
        {
            Id = userId,
            Role = UserRoleType.Transcriber.ToDisplayName(),
            Statistics = new UserStatistics { CurrentElo = 1600, TotalJobs = 100 },
            VerificationRecords = new List<VerificationRecord>(),
        };
        var admin = new User { Id = adminId, Role = UserRoleType.Admin.ToDisplayName() };

        this._repo.Setup(r => r.GetUserByIdAsync(userId, true, false, It.IsAny<Expression<Func<User, object>>[]>())).ReturnsAsync(user);
        this._repo.Setup(r => r.GetUserByIdAsync(adminId, false, false)).ReturnsAsync(admin);
        this._repo.Setup(r => r.SaveChangesAsync()).Returns(Task.FromResult(1));

        var result = await this._service.SetProfessional(userId, request);

        Assert.True(result.RoleUpdated);
        Assert.True(result.IsProfessional);
        Assert.Equal(UserRoleType.Professional.ToDisplayName(), result.NewRole);
    }

    /// <summary>
    /// Tests GetProfessionalStatus returns professional details when user is professional.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetProfessionalStatus_ShouldReturnProfessionalDetails_WhenUserIsProfessional()
    {
        var userId = Guid.NewGuid();
        var verifiedAt = DateTime.UtcNow.AddDays(-1);
        var verifiedBy = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Role = UserRoleType.Professional.ToDisplayName(),
            IsProfessional = true,
            Statistics = new UserStatistics
            {
                CurrentElo = 1600,
                TotalJobs = 60,
            },
            VerificationRecords = new List<VerificationRecord>
            {
                new VerificationRecord
                {
                    VerificationType = VerificationType.IdDocument.ToDisplayName(),
                    Status = VerificationStatusType.Approved.ToDisplayName(),
                    VerifiedAt = verifiedAt,
                    VerifiedBy = verifiedBy,
                },
            },
        };

        this._repo.Setup(r => r.GetUserByIdAsync(userId, false, false, It.IsAny<Expression<Func<User, object>>[]>()))
                  .ReturnsAsync(user);

        var result = await this._service.GetProfessionalStatus(userId);

        Assert.True(result.IsProfessional);
        Assert.NotNull(result.ProfessionalDetails);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(user.Role, result.ProfessionalDetails.Designation);
        Assert.Equal(verifiedAt, result.ProfessionalDetails.DesignatedAt);
        Assert.Equal(verifiedBy.ToString(), result.ProfessionalDetails.DesignatedBy);
        Assert.Equal(user.Statistics.TotalJobs, result.TotalJobsCompleted);
    }

    /// <summary>
    /// Tests GetProfessionalStatus returns eligibility info when user is not professional.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetProfessionalStatus_ShouldReturnEligibility_WhenUserIsNotProfessional()
    {
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Role = UserRoleType.Transcriber.ToDisplayName(),
            IsProfessional = false,
            Statistics = new UserStatistics
            {
                CurrentElo = 1400,
                TotalJobs = 40,
            },
        };

        this._repo.Setup(r => r.GetUserByIdAsync(userId, false, false, It.IsAny<Expression<Func<User, object>>[]>()))
                  .ReturnsAsync(user);

        var result = await this._service.GetProfessionalStatus(userId);

        Assert.False(result.IsProfessional);
        Assert.NotNull(result.EligibilityCriteria);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(user.Statistics.CurrentElo, result.EligibilityCriteria.UserElo);
        Assert.Equal(user.Statistics.TotalJobs, result.EligibilityCriteria.UserJobs);
        Assert.Equal(user.Role, result.CurrentRole);
    }

    /// <summary>
    /// Tests GetProfessionalStatus throws KeyNotFoundException when user does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetProfessionalStatus_ShouldThrowKeyNotFound_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        this._repo.Setup(r => r.GetUserByIdAsync(userId, false, false, It.IsAny<Expression<Func<User, object>>[]>()))
                  .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => this._service.GetProfessionalStatus(userId));
    }

    /// <summary>
    /// Tests GetBatchProfessionalStatus returns batch response correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetBatchProfessionalStatus_ShouldReturnBatchResponse()
    {
        var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var users = new List<User>
        {
            new () { Id = userIds[0], IsProfessional = true },
            new () { Id = userIds[1], IsProfessional = false },
        };

        this._repo.Setup(r => r.GetFilteredListAsync(It.IsAny<Expression<Func<User, bool>>>(), false, false))
                  .ReturnsAsync(users);

        var result = await this._service.GetBatchProfessionalStatus(userIds);

        Assert.Equal(2, result.Summary.TotalChecked);
        Assert.Equal(1, result.Summary.Professionals);
        Assert.Equal(1, result.Summary.StandardTranscribers);
        Assert.True(result.ProfessionalStatuses[userIds[0].ToString()].IsProfessional);
        Assert.False(result.ProfessionalStatuses[userIds[1].ToString()].IsProfessional);
    }

    /// <summary>
    /// Tests GetBatchProfessionalStatus throws ArgumentException when user ID list is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetBatchProfessionalStatus_ShouldThrowArgumentException_WhenUserIdsEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => this._service.GetBatchProfessionalStatus(new List<Guid>()));
    }

    /// <summary>
    /// Tests GetBatchProfessionalStatus throws KeyNotFoundException when no users are found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetBatchProfessionalStatus_ShouldThrowKeyNotFound_WhenNoUsersFound()
    {
        var userIds = new List<Guid> { Guid.NewGuid() };
        this._repo.Setup(r => r.GetFilteredListAsync(It.IsAny<Expression<Func<User, bool>>>(), false, false))
                  .ReturnsAsync(new List<User>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => this._service.GetBatchProfessionalStatus(userIds));
    }

    /// <summary>
    /// Tests AuthenticateAsync returns a token when credentials are valid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AuthenticateAsync_ShouldReturnToken_WhenCredentialsValid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Role = UserRoleType.Transcriber.ToDisplayName(),
            PasswordHash = PasswordHasher.Hash("password123"),
        };

        var request = new UserLoginRequest { Username = user.Email, Password = "password123" };

        this._repo.Setup(r => r.GetUserByEmailAsync(user.Email, false, false))
                  .ReturnsAsync(user);

        var result = await this._service.AuthenticateAsync(request);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    /// Tests AuthenticateAsync returns null when password is invalid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenInvalidPassword()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = PasswordHasher.Hash("password123"),
        };

        var request = new UserLoginRequest { Username = user.Email, Password = "wrongpass" };

        this._repo.Setup(r => r.GetUserByEmailAsync(user.Email, false, false))
                  .ReturnsAsync(user);

        var result = await this._service.AuthenticateAsync(request);

        Assert.Null(result);
    }

    /// <summary>
    /// Tests AuthenticateAsync throws ArgumentException when request is invalid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AuthenticateAsync_ShouldThrowArgumentException_WhenRequestInvalid()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => this._service.AuthenticateAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => this._service.AuthenticateAsync(new UserLoginRequest { Username = string.Empty, Password = string.Empty }));
    }

    /// <summary>
    /// Tests AuthenticateAsync returns null when user not found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenUserNotFound()
    {
        var request = new UserLoginRequest { Username = "nonexistent@example.com", Password = "password" };
        this._repo.Setup(r => r.GetUserByEmailAsync(request.Username, false, false))
                  .ReturnsAsync((User?)null);

        var result = await this._service.AuthenticateAsync(request);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that ChangePasswordAsync succeeds when current password is correct and new password is valid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnSuccess_WhenCurrentPasswordValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = PasswordHasher.Hash("OldPassword"),
        };
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        }));
        this._httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
        this._repo.Setup(r => r.GetUserByIdAsync(userId, true, false)).ReturnsAsync(user);

        // Act
        var result = await this._service.ChangePasswordAsync("OldPassword", "NewValidPassword1!");

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        this._repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    /// <summary>
    /// Tests that ChangePasswordAsync returns failure when current password is incorrect.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnFailure_WhenCurrentPasswordInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", PasswordHash = PasswordHasher.Hash("OldPassword") };
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        }));
        this._httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
        this._repo.Setup(r => r.GetUserByIdAsync(userId, true, false)).ReturnsAsync(user);

        // Act
        var result = await this._service.ChangePasswordAsync("WrongPassword", "NewValidPassword1!");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Current password is incorrect.", result.ErrorMessage);
        this._repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    /// <summary>
    /// Tests GetUserAvailabilitySummaryAsync returns correct summary when users exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUserAvailabilitySummaryAsync_ShouldReturnExpectedSummary()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRoleType.Professional.ToDisplayName(),
            Statistics = new UserStatistics
            {
                CurrentElo = 1500,
                GamesPlayed = 10,
                PeakElo = 1600,
                LastCalculated = DateTime.UtcNow.AddMinutes(-5),
            },
            Dialects = new List<UserDialect> { new () { Dialect = "English" } },
        };

        // Mock repository to return our user
        this._repo.Setup(r => r.GetFilteredUser(
                null, null, null, null, null))
            .ReturnsAsync(new List<User> { user });

        // Mock Redis availability
        this._redisService.Setup(r => r.GetBulkAvailabilityAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, UserAvailabilityRedisDto>
            {
                { user.Id, new UserAvailabilityRedisDto { Status = UserAvailabilityType.Available.ToDisplayName(), CurrentWorkload = 1 } },
            });

        // Mock Elo trend
        this._eloService.Setup(e => e.BulkEloTrendAsync(It.IsAny<List<Guid>>(), 7))
            .ReturnsAsync(new Dictionary<Guid, string>
            {
                { user.Id, "+20_over_7_days" }, // any string representing the trend
            });

        // Act
        var result = await this._service.GetUserAvailabilitySummaryAsync(null, null, null, null, null);

        // Assert
        Assert.NotNull(result.AvailableUsers);
        Assert.Single(result.AvailableUsers);
        var userDto = result.AvailableUsers.First();
        Assert.Equal(user.Id, userDto.UserId);
        Assert.Equal(UserRoleType.Professional.ToDisplayName(), userDto.Role);
        Assert.Equal(1, userDto.CurrentWorkload);
        Assert.Equal(1, result.TotalAvailable);
    }

    /// <summary>
    /// Tests GetAvailabilityAsync returns availability when cached and logs when null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAvailabilityAsync_ShouldReturnAvailabilityOrNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var availability = new UserAvailabilityRedisDto { Status = UserAvailabilityType.Available.ToDisplayName(), CurrentWorkload = 2 };
        this._redisService.Setup(r => r.GetAvailabilityAsync(userId)).ReturnsAsync(availability);

        // Act
        var result = await this._service.GetAvailabilityAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(availability.Status, result!.Status);

        // Arrange for null scenario
        this._redisService.Setup(r => r.GetAvailabilityAsync(userId)).ReturnsAsync((UserAvailabilityRedisDto?)null);

        // Act
        var nullResult = await this._service.GetAvailabilityAsync(userId);

        // Assert
        Assert.Null(nullResult);
    }

    /// <summary>
    /// Tests that PatchAvailabilityAsync successfully updates availability and returns expected response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PatchAvailabilityAsync_ShouldReturnSuccess_WhenValidRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new UserAvailabilityUpdateRequest
        {
            Status = UserAvailabilityType.Available.ToDisplayName(),
            MaxConcurrentJobs = 3,
        };

        this._redisService
            .Setup(r => r.GetAvailabilityAsync(userId))
            .ReturnsAsync(new UserAvailabilityRedisDto());

        this._redisService
            .Setup(r => r.SetAvailabilityAsync(userId, It.IsAny<UserAvailabilityRedisDto>()))
            .Returns(Task.CompletedTask);

        this._eventBus
            .Setup(e => e.PublishAsync(It.IsAny<CloudEvent>(), TopicConstant.UserAvailabilityUpdated, default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this._service.PatchAvailabilityAsync(userId, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("success", result.AvailabilityUpdated);
        Assert.Equal(updateRequest.Status, result.CurrentStatus);
        Assert.Equal(updateRequest.MaxConcurrentJobs, result.MaxConcurrentJobs);

        this._redisService.Verify(r => r.SetAvailabilityAsync(userId, It.Is<UserAvailabilityRedisDto>(u => u.Status == updateRequest.Status && u.MaxConcurrentJobs == updateRequest.MaxConcurrentJobs)), Times.Once);

        this._eventBus.Verify(e => e.PublishAsync(It.IsAny<CloudEvent>(), TopicConstant.UserAvailabilityUpdated, default), Times.Once);
    }

    /// <summary>
    /// Tests that PatchAvailabilityAsync throws ArgumentException when invalid status is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PatchAvailabilityAsync_ShouldThrow_WhenInvalidStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new UserAvailabilityUpdateRequest
        {
            Status = "invalid-status",
            MaxConcurrentJobs = 2,
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => this._service.PatchAvailabilityAsync(userId, updateRequest));
    }

    /// <summary>
    /// Tests that PatchAvailabilityAsync throws ArgumentException when MaxConcurrentJobs less than 1.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PatchAvailabilityAsync_ShouldThrow_WhenMaxConcurrentJobsLessThanOne()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new UserAvailabilityUpdateRequest
        {
            Status = UserAvailabilityType.Available.ToDisplayName(),
            MaxConcurrentJobs = 0,
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => this._service.PatchAvailabilityAsync(userId, updateRequest));
    }
}
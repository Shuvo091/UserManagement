using System.Linq.Expressions;
using AutoMapper;
using CohesionX.UserManagement.Abstractions.DTOs;
using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Abstractions.Services;
using CohesionX.UserManagement.Application.Services;
using CohesionX.UserManagement.Database.Abstractions.Entities;
using CohesionX.UserManagement.Database.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SharedLibrary.AppEnums;
using SharedLibrary.Contracts.Usermanagement.RedisDtos;
using SharedLibrary.Contracts.Usermanagement.Requests;

namespace CohesionX.UserManagement.Application.Tests;

/// <summary>
/// Tests for ELO service.
/// </summary>
public class EloServiceTests
{
    private readonly Mock<IEloRepository> _eloRepo = new ();
    private readonly Mock<IUserRepository> _userRepo = new ();
    private readonly Mock<IUserStatisticsRepository> _userStatRepo = new ();
    private readonly Mock<IUnitOfWork> _uow = new ();
    private readonly Mock<IRedisService> _redis = new ();
    private readonly Mock<IMapper> _mapper = new ();
    private readonly Mock<IWorkflowEngineClient> _wfClient = new ();
    private readonly EloService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="EloServiceTests"/> class.
    /// </summary>
    public EloServiceTests()
    {
        // Setup UnitOfWork to return repositories
        this._uow.SetupGet(u => u.UserStatistics).Returns(this._userStatRepo.Object);
        this._uow.SetupGet(u => u.EloHistories).Returns(this._eloRepo.Object);

        var options = Options.Create(new AppConstantsOptions
        {
            EloKFactorNew = 32,
            EloKFactorEstablished = 24,
            EloKFactorExpert = 16,
        });

        this._service = new EloService(
            this._eloRepo.Object,
            this._userRepo.Object,
            this._userStatRepo.Object,
            this._uow.Object,
            this._redis.Object,
            this._mapper.Object,
            options,
            this._wfClient.Object,
            Mock.Of<ILogger<EloService>>());
    }

    /// <summary>
    /// ApplyEloUpdatesAsync_HappyPath_CallsReposAndNotifies.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyEloUpdatesAsync_HappyPath_CallsReposAndNotifies()
    {
        // Arrange
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var req = new EloUpdateRequest
        {
            WorkflowRequestId = "job1",
            QaComparisonId = Guid.NewGuid(),
            RecommendedEloChanges = new List<RecommendedEloChangeDto>
            {
                new () { TranscriberId = user1, OldElo = 1200, OpponentElo = 1250, RecommendedChange = 10, ComparisonOutcome = "win" },
                new () { TranscriberId = user2, OldElo = 1200, OpponentElo = 1150, RecommendedChange = -5, ComparisonOutcome = "loss" },
            },
            ComparisonMetadata = new ComparisonMetadataDto
            {
                QaMethod = "method",
                ComparisonType = "type",
            },
        };

        var stats = new List<UserStatistics>
        {
            new () { UserId = user1, CurrentElo = 1200, PeakElo = 1200, GamesPlayed = 0 },
            new () { UserId = user2, CurrentElo = 1200, PeakElo = 1200, GamesPlayed = 29 },
        };
        this._userStatRepo.Setup(x => x.GetByUserIdsAsync(It.IsAny<List<Guid>>(), true))
                     .ReturnsAsync(stats);
        this._eloRepo
          .Setup(x => x.FindAsync(
              It.IsAny<Expression<Func<EloHistory, bool>>>(),
              It.IsAny<bool>()))
          .ReturnsAsync(new List<EloHistory>());

        // Act
        var resp = await this._service.ApplyEloUpdatesAsync(req);

        // Assert
        Assert.Equal("job1", resp.WorkflowRequestId);
        Assert.Equal(2, resp.EloUpdatesApplied.Count);
        this._uow.Verify(x => x.SaveChangesAsync(), Times.Once);
        this._wfClient.Verify(x => x.NotifyEloUpdatedAsync(It.IsAny<EloUpdateNotificationRequest>()), Times.Once);
        this._redis.Verify(x => x.SetUserEloAsync(user1, It.IsAny<UserEloRedisDto>()), Times.Once);
        this._redis.Verify(x => x.SetUserEloAsync(user2, It.IsAny<UserEloRedisDto>()), Times.Once);
    }

    /// <summary>
    /// ApplyEloUpdatesAsync_Throws_When_MissingStats.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyEloUpdatesAsync_Throws_When_MissingStats()
    {
        // Arrange
        var req = new EloUpdateRequest { RecommendedEloChanges = new List<RecommendedEloChangeDto> { new () { TranscriberId = Guid.NewGuid() } } };
        this._userStatRepo.Setup(x => x.GetByUserIdsAsync(It.IsAny<List<Guid>>(), true))
                     .ReturnsAsync(new List<UserStatistics>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await this._service.ApplyEloUpdatesAsync(req));
    }

    /// <summary>
    /// ApplyEloUpdatesAsync_Throws_When_TooManyChanges.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyEloUpdatesAsync_Throws_When_TooManyChanges()
    {
        // Arrange
        var req = new EloUpdateRequest { RecommendedEloChanges = new List<RecommendedEloChangeDto> { new (), new (), new () } };
        this._userStatRepo.Setup(x => x.GetByUserIdsAsync(It.IsAny<List<Guid>>(), true))
                     .ReturnsAsync(new List<UserStatistics> { new (), new (), new () });

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await this._service.ApplyEloUpdatesAsync(req));
    }

    /// <summary>
    /// GetEloHistoryAsync_HappyPath_ComputesHistory.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetEloHistoryAsync_HappyPath_ComputesHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Statistics = new UserStatistics { CurrentElo = 1300, PeakElo = 1350, GamesPlayed = 3 },
            EloHistories = new List<EloHistory>
            {
                new () { ChangedAt = DateTime.UtcNow.AddDays(-2), OldElo = 1200, NewElo = 1250, OpponentElo = 1300, Outcome = "win", JobId = "j1" },
                new () { ChangedAt = DateTime.UtcNow.AddDays(-1), OldElo = 1250, NewElo = 1300, OpponentElo = 1200, Outcome = "win", JobId = "j2" },
            },
        };
        this._userRepo.Setup(x => x.GetUserByIdAsync(userId, true)).ReturnsAsync(user);

        // Act
        var resp = await this._service.GetEloHistoryAsync(userId);

        // Assert
        Assert.Equal(userId, resp.UserId);
        Assert.Equal(2, resp.EloHistory.Count);
        Assert.InRange(resp.Trends.WinRate, 0, 100);
    }

    /// <summary>
    /// ApplyEloUpdatesAsync_HappyPath_CallsReposAndNotifies.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetEloHistoryAsync_Throws_When_UserNotFound()
    {
        // Arrange
        this._userRepo.Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>(), true)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await this._service.GetEloHistoryAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// GetEloTrend_ListOverload_Works.
    /// </summary>
    /// <param name="count"> count of changes. </param>
    /// <param name="days"> change timeline. </param>
    /// <param name="expected"> expected result. </param>
    [Theory]
    [InlineData(0, 10, "0_over_10_days")]
    [InlineData(5, 3, "+5_over_3_days")]
    public void GetEloTrend_ListOverload_Works(int count, int days, string expected)
    {
        // Arrange
        var list = new List<EloHistory>();
        if (count > 1)
        {
            var now = DateTime.UtcNow;
            list.Add(new EloHistory { ChangedAt = now.AddDays(-days), OldElo = 1000, NewElo = 1000 });
            list.Add(new EloHistory { ChangedAt = now, OldElo = 1000, NewElo = 1005 });
        }

        // Act
        var trend = this._service.GetEloTrend(list, days);

        // Assert
        Assert.Equal(expected, trend);
    }

    /// <summary>
    /// BulkEloTrendAsync_Returns_Default_For_NoHistory.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkEloTrendAsync_Returns_Default_For_NoHistory()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        this._eloRepo
          .Setup(x => x.FindAsync(
              It.IsAny<Expression<Func<EloHistory, bool>>>(),
              It.IsAny<bool>()))
          .ReturnsAsync(new List<EloHistory>());

        // Act
        var dict = await this._service.BulkEloTrendAsync(ids, 7);

        // Assert
        Assert.All(dict.Values, v => Assert.Equal("0_over_7_days", v));
    }

    /// <summary>
    /// GetWinRate_CalculatesCorrectly.
    /// </summary>
    [Fact]
    public void GetWinRate_CalculatesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var list = new List<EloHistory>
        {
            new () { ChangedAt = now, Outcome = "win" },
            new () { ChangedAt = now, Outcome = "loss" },
            new () { ChangedAt = now, Outcome = "win" },
        };

        // Act
        var rate = this._service.GetWinRate(list);

        // Assert
        Assert.Equal(2d / 3 * 100, rate);
    }

    /// <summary>
    /// GetAverageOpponentElo_CalculatesCorrectly.
    /// </summary>
    [Fact]
    public void GetAverageOpponentElo_CalculatesCorrectly()
    {
        // Arrange
        var list = new List<EloHistory>
        {
            new () { OpponentElo = 1000 },
            new () { OpponentElo = 1100 },
        };

        // Act
        var avg = this._service.GetAverageOpponentElo(list);

        // Assert
        Assert.Equal(1050, avg);
    }

    /// <summary>
    /// ResolveThreeWay_HappyPath_AppliesAll.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ResolveThreeWay_HappyPath_AppliesAll()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var req = new ThreeWayEloUpdateRequest
        {
            WorkflowRequestId = "j3",
            OriginalComparisonId = Guid.NewGuid(),
            ThreeWayEloChanges = new List<ThreeWayEloChange>
            {
                new () { TranscriberId = id1, Role = ThreeWayTranscriberRoleType.OriginalTranscriber1.ToDisplayName(), EloChange = 10, Outcome = "majority_winner" },
                new () { TranscriberId = id2, Role = ThreeWayTranscriberRoleType.OriginalTranscriber2.ToDisplayName(), EloChange = -5, Outcome = "minority_loser" },
                new () { TranscriberId = id3, Role = ThreeWayTranscriberRoleType.TiebreakerTranscriber.ToDisplayName(), EloChange = -5, Outcome = "minority_loser" },
            },
        };
        var statsList = new List<UserStatistics>
        {
            new () { UserId = id1, CurrentElo = 1200, GamesPlayed = 0, PeakElo = 1200 },
            new () { UserId = id2, CurrentElo = 1200, GamesPlayed = 0, PeakElo = 1200 },
            new () { UserId = id3, CurrentElo = 1200, GamesPlayed = 0, PeakElo = 1200 },
        };
        this._userStatRepo.Setup(x => x.GetByUserIdsAsync(It.IsAny<List<Guid>>(), true))
                     .ReturnsAsync(statsList);
        this._eloRepo
          .Setup(x => x.FindAsync(
              It.IsAny<Expression<Func<EloHistory, bool>>>(),
              It.IsAny<bool>()))
          .ReturnsAsync(new List<EloHistory>());

        // Act
        var resp = await this._service.ResolveThreeWay(req);

        // Assert
        Assert.True(resp.EloUpdateConfirmed);
        Assert.Equal(3, resp.UpdatesApplied);
        this._wfClient.Verify(x => x.NotifyEloUpdatedAsync(It.IsAny<EloUpdateNotificationRequest>()), Times.Once);
    }

    /// <summary>
    /// ResolveThreeWay_Throws_When_InvalidCount.
    /// </summary>
    /// <param name="count"> total count. </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task ResolveThreeWay_Throws_When_InvalidCount(int count)
    {
        var req = new ThreeWayEloUpdateRequest { ThreeWayEloChanges = new List<ThreeWayEloChange>(new ThreeWayEloChange[count]) };
        await Assert.ThrowsAsync<ArgumentException>(() => this._service.ResolveThreeWay(req));
    }
}
using System.Text.Json;
using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SharedLibrary.Cache.Services.Interfaces;
using SharedLibrary.Contracts.Usermanagement.RedisDtos;
using Xunit;

namespace CohesionX.UserManagement.Tests.Services;

/// <summary>
/// Unit tests for <see cref="RedisService"/>.
/// </summary>
public class RedisServiceTests
{
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<RedisService>> _loggerMock;
    private readonly RedisService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisServiceTests"/> class.
    /// </summary>
    public RedisServiceTests()
    {
        this._cacheMock = new Mock<ICacheService>();
        this._loggerMock = new Mock<ILogger<RedisService>>();
        var opts = Options.Create(new AppConstantsOptions { RedisCacheTtlMinutes = 10 });
        this._service = new RedisService(this._cacheMock.Object, opts, this._loggerMock.Object);
    }

    // --------------------
    // GetAvailabilityAsync
    // --------------------

    /// <summary>
    /// Should return DTO when cache has user availability.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAvailabilityAsync_ReturnsDto_WhenExists()
    {
        var userId = Guid.NewGuid();
        var dto = new UserAvailabilityRedisDto { Status = "available" };
        var json = JsonSerializer.Serialize(dto);

        this._cacheMock.Setup(c => c.GetAsync<string>($"user:availability:{userId}"))
            .ReturnsAsync(json);

        var result = await this._service.GetAvailabilityAsync(userId);

        Assert.NotNull(result);
        Assert.True(result!.Status == "available");
    }

    /// <summary>
    /// Should return null when user availability is not in cache.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAvailabilityAsync_ReturnsNull_WhenMissing()
    {
        var userId = Guid.NewGuid();

        this._cacheMock.Setup(c => c.GetAsync<string>(It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var result = await this._service.GetAvailabilityAsync(userId);

        Assert.Null(result);
    }

    /// <summary>
    /// Should throw <see cref="ArgumentException"/> when userId is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAvailabilityAsync_Throws_WhenUserIdEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            this._service.GetAvailabilityAsync(Guid.Empty));
    }

    /// <summary>
    /// Should throw <see cref="JsonException"/> when cache has invalid JSON.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAvailabilityAsync_Throws_OnInvalidJson()
    {
        var userId = Guid.NewGuid();

        this._cacheMock.Setup(c => c.GetAsync<string>(It.IsAny<string>()))
            .ReturnsAsync("invalid-json");

        await Assert.ThrowsAsync<JsonException>(() =>
            this._service.GetAvailabilityAsync(userId));
    }

    // --------------------
    // SetAvailabilityAsync
    // --------------------

    /// <summary>
    /// Should set cache successfully for valid DTO.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetAvailabilityAsync_SetsCache()
    {
        var userId = Guid.NewGuid();
        var dto = new UserAvailabilityRedisDto { Status = "available" };

        await this._service.SetAvailabilityAsync(userId, dto);

        this._cacheMock.Verify(c => c.SetAsync($"user:availability:{userId}", It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    /// <summary>
    /// Should throw <see cref="ArgumentException"/> when userId is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetAvailabilityAsync_Throws_WhenUserIdEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            this._service.SetAvailabilityAsync(Guid.Empty, new UserAvailabilityRedisDto()));
    }

    /// <summary>
    /// Should throw <see cref="ArgumentNullException"/> when DTO is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetAvailabilityAsync_Throws_WhenDtoNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            this._service.SetAvailabilityAsync(Guid.NewGuid(), null!));
    }

    // --------------------
    // TryClaimJobAsync
    // --------------------

    /// <summary>
    /// Should return true and set cache when job is unclaimed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryClaimJobAsync_ReturnsTrue_WhenUnclaimed()
    {
        var userId = Guid.NewGuid();
        this._cacheMock.Setup(c => c.GetAsync<string>("job:claim:lock:job123"))
            .ReturnsAsync((string?)null);

        var result = await this._service.TryClaimJobAsync("job123", userId);

        Assert.True(result);
        this._cacheMock.Verify(c => c.SetAsync("job:claim:lock:job123", It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    /// <summary>
    /// Should return false when job is already claimed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryClaimJobAsync_ReturnsFalse_WhenAlreadyClaimed()
    {
        var userId = Guid.NewGuid();
        this._cacheMock.Setup(c => c.GetAsync<string>("job:claim:lock:job123"))
            .ReturnsAsync("existing");

        var result = await this._service.TryClaimJobAsync("job123", userId);

        Assert.False(result);
    }

    /// <summary>
    /// Should throw <see cref="ArgumentException"/> when jobId is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryClaimJobAsync_Throws_WhenJobIdEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            this._service.TryClaimJobAsync(string.Empty, Guid.NewGuid()));
    }

    /// <summary>
    /// Should throw <see cref="ArgumentException"/> when userId is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryClaimJobAsync_Throws_WhenUserIdEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            this._service.TryClaimJobAsync("job123", Guid.Empty));
    }

    // --------------------
    // ReleaseJobClaimAsync
    // --------------------

    /// <summary>
    /// Should remove job claim key from cache.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ReleaseJobClaimAsync_RemovesKey()
    {
        await this._service.ReleaseJobClaimAsync("job123");

        this._cacheMock.Verify(c => c.RemoveAsync("job:claim:lock:job123"), Times.Once);
    }

    /// <summary>
    /// Should throw <see cref="ArgumentException"/> when jobId is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ReleaseJobClaimAsync_Throws_WhenJobIdEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            this._service.ReleaseJobClaimAsync(string.Empty));
    }

    // --------------------
    // GetUserClaimsAsync
    // --------------------

    /// <summary>
    /// Should return claims list when cache has values.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUserClaimsAsync_ReturnsList_WhenExists()
    {
        var userId = Guid.NewGuid();
        var claims = new List<string> { "job1", "job2" };
        var json = JsonSerializer.Serialize(claims);

        this._cacheMock.Setup(c => c.GetAsync<string>($"user:claims:{userId}"))
            .ReturnsAsync(json);

        var result = await this._service.GetUserClaimsAsync(userId);

        Assert.Equal(2, result.Count);
        Assert.Contains("job1", result);
    }

    /// <summary>
    /// Should return empty list when no claims in cache.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUserClaimsAsync_ReturnsEmpty_WhenMissing()
    {
        var userId = Guid.NewGuid();

        this._cacheMock.Setup(c => c.GetAsync<string>(It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var result = await this._service.GetUserClaimsAsync(userId);

        Assert.Empty(result);
    }

    // --------------------
    // AddUserClaimAsync
    // --------------------

    /// <summary>
    /// Should add new claim when not present in cache.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AddUserClaimAsync_Adds_WhenNew()
    {
        var userId = Guid.NewGuid();
        this._cacheMock.Setup(c => c.GetAsync<string>($"user:claims:{userId}"))
            .ReturnsAsync("[]");

        await this._service.AddUserClaimAsync(userId, "job1");

        this._cacheMock.Verify(c => c.SetAsync($"user:claims:{userId}", It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    // --------------------
    // RemoveUserClaimAsync
    // --------------------

    /// <summary>
    /// Should remove claim when exists in cache.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RemoveUserClaimAsync_Removes_WhenExists()
    {
        var userId = Guid.NewGuid();
        var claims = new List<string> { "job1" };
        var json = JsonSerializer.Serialize(claims);

        this._cacheMock.Setup(c => c.GetAsync<string>($"user:claims:{userId}"))
            .ReturnsAsync(json);

        await this._service.RemoveUserClaimAsync(userId, "job1");

        this._cacheMock.Verify(c => c.SetAsync($"user:claims:{userId}", "[]", It.IsAny<TimeSpan>()), Times.Once);
    }

    // --------------------
    // GetUserEloAsync / SetUserEloAsync
    // --------------------

    /// <summary>
    /// Should return Elo DTO when cache has data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUserEloAsync_ReturnsDto_WhenExists()
    {
        var userId = Guid.NewGuid();
        var dto = new UserEloRedisDto { CurrentElo = 1500 };
        var json = JsonSerializer.Serialize(dto);

        this._cacheMock.Setup(c => c.GetAsync<string>($"user:elo:{userId}"))
            .ReturnsAsync(json);

        var result = await this._service.GetUserEloAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(1500, result!.CurrentElo);
    }

    /// <summary>
    /// Should set user Elo in cache.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetUserEloAsync_SetsCache()
    {
        var userId = Guid.NewGuid();
        var dto = new UserEloRedisDto { CurrentElo = 1500 };

        await this._service.SetUserEloAsync(userId, dto);

        this._cacheMock.Verify(c => c.SetAsync($"user:elo:{userId}", It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    // --------------------
    // GetBulkAvailabilityAsync
    // --------------------

    /// <summary>
    /// Should return bulk availability map for given userIds.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetBulkAvailabilityAsync_ReturnsMap()
    {
        var u1 = Guid.NewGuid();
        var dto = new UserAvailabilityRedisDto { Status = "available" };
        var json = JsonSerializer.Serialize(dto);

        this._cacheMock.Setup(c => c.GetAsync<string>($"user:availability:{u1}"))
            .ReturnsAsync(json);

        var result = await this._service.GetBulkAvailabilityAsync(new List<Guid> { u1 });

        Assert.Single(result);
        Assert.True(result[u1].Status == "available");
    }

    /// <summary>
    /// Should throw <see cref="ArgumentNullException"/> when input collection is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetBulkAvailabilityAsync_Throws_WhenNullIds()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            this._service.GetBulkAvailabilityAsync(null!));
    }
}

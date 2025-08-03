using System.Reflection;
using System.Text.Json;
using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SharedLibrary.Cache.Services.Interfaces;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Application.Tests;

/// <summary>
/// RedisServiceTests.
/// </summary>
public class RedisServiceTests
{
    private readonly Mock<ICacheService> _cache = new ();
    private readonly RedisService _service;
    private readonly TimeSpan _expectedAvailabilityTtl;
    private readonly TimeSpan _userClaimsTtl;
    private readonly TimeSpan _jobClaimLockTtl;
    private readonly TimeSpan _userEloTtl;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisServiceTests"/> class.
    /// </summary>
    public RedisServiceTests()
    {
        this._expectedAvailabilityTtl = TimeSpan.FromMinutes(360);
        this._jobClaimLockTtl = TimeSpan.FromSeconds(30);
        this._userClaimsTtl = TimeSpan.FromHours(8);
        this._userEloTtl = TimeSpan.FromHours(1);

        var opts = Options.Create(new AppConstantsOptions
        {
            RedisCacheTtlMinutes = 360,
        });

        this._service = new RedisService(this._cache.Object, opts, Mock.Of<ILogger<RedisService>>());
    }

    /// <summary>
    /// GetAvailabilityAsync_ReturnsNull_WhenEmpty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task GetAvailabilityAsync_ReturnsNull_WhenEmpty()
    {
        var userId = Guid.NewGuid();
        this._cache.Setup(c => c.GetAsync<string>(It.IsAny<string>()))
              .ReturnsAsync((string?)null);

        var result = await this._service.GetAvailabilityAsync(userId);
        Assert.Null(result);
    }

    /// <summary>
    /// GetAvailabilityAsync_DeserializesDto_WhenPresent.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task GetAvailabilityAsync_DeserializesDto_WhenPresent()
    {
        var userId = Guid.NewGuid();
        var dto = new UserAvailabilityRedisDto { Status = "available", MaxConcurrentJobs = 3, CurrentWorkload = 1, LastUpdate = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(dto);
        var key = InvokePrivateKey<string>("GetAvailabilityKey", userId);
        this._cache.Setup(c => c.GetAsync<string>(key)).ReturnsAsync(json);

        var result = await this._service.GetAvailabilityAsync(userId);
        Assert.NotNull(result);
        Assert.True(result.Status == "available");
    }

    /// <summary>
    /// SetAvailabilityAsync_SerializesAndSetsWithTtl.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task SetAvailabilityAsync_SerializesAndSetsWithTtl()
    {
        var userId = Guid.NewGuid();
        var dto = new UserAvailabilityRedisDto { Status = "busy", MaxConcurrentJobs = 3, CurrentWorkload = 1, LastUpdate = DateTime.UtcNow };
        string? key = null, value = null;
        TimeSpan? ttl = null;
        this._cache.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>()))
              .Callback<string, string, TimeSpan?>((k, v, t) =>
              {
                  key = k;
                  value = v;
                  ttl = t;
              });

        await this._service.SetAvailabilityAsync(userId, dto);
        Assert.Contains(userId.ToString(), key);
        Assert.Equal(JsonSerializer.Serialize(dto), value);
        Assert.Equal(this._expectedAvailabilityTtl, ttl);
    }

    /// <summary>
    /// TryClaimJobAsync_ReturnsFalse_WhenAlreadyClaimed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task TryClaimJobAsync_ReturnsFalse_WhenAlreadyClaimed()
    {
        var job = "job1";
        var key = InvokePrivateKey<string>("GetJobClaimLockKey", job);
        this._cache.Setup(c => c.GetAsync<string>(key)).ReturnsAsync("existing");

        var result = await this._service.TryClaimJobAsync(job, Guid.NewGuid());
        Assert.False(result);
        this._cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), this._jobClaimLockTtl), Times.Never);
    }

    /// <summary>
    /// TryClaimJobAsync_SetsAndReturnsTrue_WhenNotClaimed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task TryClaimJobAsync_SetsAndReturnsTrue_WhenNotClaimed()
    {
        var job = "job2";
        this._cache.Setup(c => c.GetAsync<string>(It.IsAny<string>())).ReturnsAsync((string?)null);
        this._cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), this._jobClaimLockTtl));

        var userId = Guid.NewGuid();
        var result = await this._service.TryClaimJobAsync(job, userId);
        Assert.True(result);
        this._cache.Verify(
            c => c.SetAsync(
            It.Is<string>(k => k.Contains(job)),
            It.Is<string>(v => v.Contains(userId.ToString())),
            this._jobClaimLockTtl), Times.Once);
    }

    /// <summary>
    /// ReleaseJobClaimAsync_CallsRemove.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task ReleaseJobClaimAsync_CallsRemove()
    {
        var job = "job3";
        this._cache.Setup(c => c.RemoveAsync(It.IsAny<string>()));
        await this._service.ReleaseJobClaimAsync(job);
        this._cache.Verify(c => c.RemoveAsync(It.Is<string>(k => k.Contains(job))), Times.Once);
    }

    /// <summary>
    /// GetUserClaimsAsync_ReturnsEmpty_WhenNone.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task GetUserClaimsAsync_ReturnsEmpty_WhenNone()
    {
        var userId = Guid.NewGuid();
        this._cache.Setup(c => c.GetAsync<string>(It.IsAny<string>())).ReturnsAsync(string.Empty);
        var list = await this._service.GetUserClaimsAsync(userId);
        Assert.Empty(list);
    }

    /// <summary>
    /// GetUserClaimsAsync_DeserializesJson.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task GetUserClaimsAsync_DeserializesJson()
    {
        var userId = Guid.NewGuid();
        var jobs = new List<string> { "j1", "j2" };
        this._cache.Setup(c => c.GetAsync<string>(It.IsAny<string>()))
              .ReturnsAsync(JsonSerializer.Serialize(jobs));
        var list = await this._service.GetUserClaimsAsync(userId);
        Assert.Equal(jobs, list);
    }

    /// <summary>
    /// AddUserClaimAsync_AddsAndSets_WhenNotAlready.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task AddUserClaimAsync_AddsAndSets_WhenNotAlready()
    {
        var userId = Guid.NewGuid();
        this._cache.Setup(c => c.GetAsync<string>(It.IsAny<string>()))
              .ReturnsAsync(JsonSerializer.Serialize(new List<string> { "a" }));
        string? setJson = null;
        this._cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), this._userClaimsTtl))
              .Callback<string, string, TimeSpan?>((_, v, __) => setJson = v);
        await this._service.AddUserClaimAsync(userId, "b");
        var result = JsonSerializer.Deserialize<List<string>>(setJson!);
        Assert.Contains("b", result!);
    }

    /// <summary>
    /// RemoveUserClaimAsync_RemovesAndSets_WhenPresent.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task RemoveUserClaimAsync_RemovesAndSets_WhenPresent()
    {
        var userId = Guid.NewGuid();
        this._cache.Setup(c => c.GetAsync<string>(It.IsAny<string>()))
              .ReturnsAsync(JsonSerializer.Serialize(new List<string> { "x", "y" }));
        string? setJson = null;
        this._cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), this._userClaimsTtl))
              .Callback<string, string, TimeSpan?>((_, v, __) => setJson = v);
        await this._service.RemoveUserClaimAsync(userId, "x");
        var result = JsonSerializer.Deserialize<List<string>>(setJson!);
        Assert.DoesNotContain("x", result!);
    }

    /// <summary>
    /// GetUserEloAsync_ReturnsNull_WhenEmpty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task GetUserEloAsync_ReturnsNull_WhenEmpty()
    {
        var userId = Guid.NewGuid();
        this._cache.Setup(c => c.GetAsync<string>(It.IsAny<string>())).ReturnsAsync((string?)null);
        var result = await this._service.GetUserEloAsync(userId);
        Assert.Null(result);
    }

    /// <summary>
    /// GetUserEloAsync_DeserializesDto.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task GetUserEloAsync_DeserializesDto()
    {
        var userId = Guid.NewGuid();
        var dto = new UserEloRedisDto { CurrentElo = 1500, PeakElo = 1550, GamesPlayed = 5, RecentTrend = "+10_over_7_days", LastJobCompleted = DateTime.UtcNow };
        this._cache.Setup(c => c.GetAsync<string>(It.IsAny<string>())).ReturnsAsync(JsonSerializer.Serialize(dto));
        var result = await this._service.GetUserEloAsync(userId);
        Assert.NotNull(result);
        Assert.Equal(1500, result.CurrentElo);
    }

    /// <summary>
    /// SetUserEloAsync_SerializesAndSets.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task SetUserEloAsync_SerializesAndSets()
    {
        var userId = Guid.NewGuid();
        var dto = new UserEloRedisDto { CurrentElo = 1400 };
        string? setValue = null;
        this._cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), this._userEloTtl))
              .Callback<string, string, TimeSpan?>((_, v, __) => setValue = v);

        await this._service.SetUserEloAsync(userId, dto);

        Assert.Equal(JsonSerializer.Serialize(dto), setValue);
    }

    /// <summary>
    /// GetBulkAvailabilityAsync_ReturnsMapOnlyForValid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task GetBulkAvailabilityAsync_ReturnsMapOnlyForValid()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var dto = new UserAvailabilityRedisDto { Status = "available", MaxConcurrentJobs = 3, CurrentWorkload = 1, LastUpdate = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(dto);
        this._cache.SetupSequence(c => c.GetAsync<string>(It.IsAny<string>()))
              .ReturnsAsync(json)
              .ReturnsAsync((string?)null);
        var result = await this._service.GetBulkAvailabilityAsync(new[] { id1, id2 });
        Assert.Single(result);
        Assert.True(result.ContainsKey(id1));
    }

    /// <summary>
    /// InvokePrivateKey.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    private static T InvokePrivateKey<T>(string methodName, object arg)
    {
        var opts = Options.Create(new AppConstantsOptions { RedisCacheTtlMinutes = 360 });
        var svc = new RedisService(null!, opts, null!);
        var mi = typeof(RedisService).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance) !;
        return (T)mi.Invoke(svc, new[] { arg }) !;
    }
}
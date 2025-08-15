// <copyright file="EloService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using AutoMapper;
using CloudNative.CloudEvents;
using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Abstractions.Services;
using CohesionX.UserManagement.Application.Constants;
using CohesionX.UserManagement.Database.Abstractions.Entities;
using CohesionX.UserManagement.Database.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedLibrary.AppEnums;
using SharedLibrary.Common.Utilities;
using SharedLibrary.Contracts.Usermanagement.RedisDtos;
using SharedLibrary.Contracts.Usermanagement.Requests;
using SharedLibrary.Contracts.Usermanagement.Responses;
using SharedLibrary.Kafka.Services.Interfaces;
using StackExchange.Redis;

namespace CohesionX.UserManagement.Application.Services;

/// <summary>
/// Provides Elo rating management, history retrieval, trend analysis, and notification operations.
/// </summary>
public class EloService : IEloService
{
    private readonly IEloRepository repo;
    private readonly IUserRepository userRepo;
    private readonly IWorkflowEngineClient workflowEngineClient;
    private readonly IUserStatisticsRepository userStatRepo;
    private readonly IMapper mapper;
    private readonly IUnitOfWork unitOfWork;
    private readonly IRedisService redisService;
    private readonly IEventBus eventBus;
    private readonly int eloKFactorNew;
    private readonly int eloKFactorEstablished;
    private readonly int eloKFactorExpert;
    private readonly ILogger<EloService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EloService"/> class.
    /// </summary>
    /// <param name="repo">Repository for accessing and managing Elo history records.</param>
    /// <param name="userRepo">Repository for user-related data operations.</param>
    /// <param name="userStatRepo">Repository for user statistics data access.</param>
    /// <param name="unitOfWork">Unit of Work pattern implementation to coordinate repository operations and transaction management.</param>
    /// <param name="redisService">Service for interacting with Redis cache and data storage.</param>
    /// <param name="eventBus">Service for publishing in kafka.</param>
    /// <param name="mapper">AutoMapper instance used for mapping between domain entities and DTOs.</param>
    /// <param name="appContantOptions"> Options for app contants. </param>
    /// <param name="workflowEngineClient">Client for communicating with the external workflow engine API.</param>
    /// <param name="logger"> logger. </param>
    public EloService(
        IEloRepository repo,
        IUserRepository userRepo,
        IUserStatisticsRepository userStatRepo,
        IUnitOfWork unitOfWork,
        IRedisService redisService,
        IEventBus eventBus,
        IMapper mapper,
        IOptions<AppConstantsOptions> appContantOptions,
        IWorkflowEngineClient workflowEngineClient,
        ILogger<EloService> logger)
    {
        this.repo = repo;
        this.userRepo = userRepo;
        this.unitOfWork = unitOfWork;
        this.redisService = redisService;
        this.eventBus = eventBus;
        this.userStatRepo = userStatRepo;
        this.mapper = mapper;
        this.eloKFactorNew = appContantOptions.Value.EloKFactorNew;
        this.eloKFactorEstablished = appContantOptions.Value.EloKFactorEstablished;
        this.eloKFactorExpert = appContantOptions.Value.EloKFactorExpert;
        this.workflowEngineClient = workflowEngineClient;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<EloUpdateResponse> ApplyEloUpdatesAsync(EloUpdateRequest request)
    {
        var eloUpdateResp = new EloUpdateResponse
        {
            WorkflowRequestId = request.WorkflowRequestId,
            ComparisonId = request.QaComparisonId,
            UpdatedAt = DateTime.UtcNow,
        };

        var userIds = request.RecommendedEloChanges
            .Select(r => r.TranscriberId)
            .Distinct()
            .ToList();

        var userStatisticsDb = await this.unitOfWork.UserStatistics
            .GetByUserIdsAsync(userIds, trackChanges: true);

        if (userStatisticsDb == null || userStatisticsDb.Count != userIds.Count)
        {
            this.logger.LogWarning($"Missing UserStatistics for some transcribers.");
            throw new ArgumentException("Missing UserStatistics for some transcribers.");
        }

        if (request.RecommendedEloChanges.Count > 2)
        {
            this.logger.LogWarning($"Missing UserStatistics for some transcribers: Found {request.RecommendedEloChanges.Count}. It should be 2.");
            throw new ArgumentException("Unexpected number of elo change request found");
        }

        var eloHistoryRecords = new List<EloHistory>();
        var jobCompletions = new List<JobCompletion>();
        var cutOffDate = DateTime.UtcNow.AddDays(-7);

        foreach (var eloChange in request.RecommendedEloChanges)
        {
            var utcNow = DateTime.UtcNow;
            var userStats = userStatisticsDb
                .FirstOrDefault(us => us.UserId == eloChange.TranscriberId)
                ?? throw new KeyNotFoundException($"User statistics not found for user {eloChange.TranscriberId}");

            var newElo = eloChange.OldElo + eloChange.RecommendedChange;

            var eloHistoryRecord = new EloHistory
            {
                UserId = eloChange.TranscriberId,
                OldElo = eloChange.OldElo,
                NewElo = newElo,
                OpponentElo = eloChange.OpponentElo,
                Reason = request.ComparisonMetadata.QaMethod ?? string.Empty,
                ComparisonId = request.QaComparisonId,
                JobId = request.WorkflowRequestId ?? string.Empty,
                Outcome = eloChange.ComparisonOutcome ?? string.Empty,
                ComparisonType = request.ComparisonMetadata.ComparisonType ?? string.Empty,
                KFactorUsed = this.CalculateKFactor(userStats.GamesPlayed),
                ChangedAt = utcNow,
            };

            var jobCompletion = new JobCompletion
            {
                UserId = eloChange.TranscriberId,
                JobId = request.WorkflowRequestId ?? string.Empty,
                Outcome = eloChange.ComparisonOutcome ?? string.Empty,
                EloChange = eloChange.RecommendedChange,
                ComparisonId = request.QaComparisonId,
                CreatedAt = utcNow,
                CompletedAt = utcNow,
            };

            // Update stats in-memory
            userStats.CurrentElo = newElo;
            userStats.PeakElo = Math.Max(userStats.PeakElo, newElo);
            userStats.GamesPlayed++;
            userStats.TotalJobs++;
            userStats.LastCalculated = eloHistoryRecord.ChangedAt;
            userStats.UpdatedAt = utcNow;

            eloHistoryRecords.Add(eloHistoryRecord);
            jobCompletions.Add(jobCompletion);

            eloUpdateResp.EloUpdatesApplied.Add(new EloUpdateAppliedDto
            {
                TranscriberId = eloChange.TranscriberId,
                OldElo = eloChange.OldElo,
                NewElo = newElo,
                EloChange = eloChange.RecommendedChange,
                ComparisonOutcome = eloChange.ComparisonOutcome,
            });

            // For redis update
            var recentHistory = await this.repo.FindAsync(
                eh => eh.UserId == eloChange.TranscriberId && eh.ChangedAt >= cutOffDate);
            recentHistory.Add(eloHistoryRecord);
            await this.redisService.SetUserEloAsync(eloChange.TranscriberId, new UserEloRedisDto
            {
                CurrentElo = newElo,
                PeakElo = userStats.PeakElo,
                GamesPlayed = userStats.GamesPlayed,
                RecentTrend = this.GetEloTrend(recentHistory, 7),
                LastJobCompleted = jobCompletion.CompletedAt,
            });
        }

        await this.unitOfWork.EloHistories.AddRangeAsync(eloHistoryRecords);
        await this.unitOfWork.JobCompletions.AddRangeAsync(jobCompletions);
        await this.unitOfWork.SaveChangesAsync();

        var cloudEvent = new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri($"{TopicConstant.UserEloUpdated}:{request.WorkflowRequestId}"),
            Type = TopicConstant.UserEloUpdated,
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json",
            Data = new { RequestId = request.WorkflowRequestId, Message = "Users Elo Updated." },
        };
        try
        {
            await this.eventBus.PublishAsync(cloudEvent, TopicConstant.UserEloUpdated);
            this.logger.LogInformation($"{request.WorkflowRequestId} CloudEvent publish Successful.");
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, $"{request.WorkflowRequestId} CloudEvent publish falied.");
        }

        var notifyEloUpdateReq = new EloUpdateNotificationRequest
        {
            UpdateId = request.WorkflowRequestId!,
            EventType = WorkflowEventType.EloUpdated.ToDisplayName(),
            EventData = new EloUpdateEventData
            {
                ComparisonId = request.QaComparisonId!,
                UsersUpdated = eloUpdateResp.EloUpdatesApplied.Count,
                UpdateResults = eloUpdateResp.EloUpdatesApplied.Select(u => new EloUpdateResult
                {
                    UserId = u.TranscriberId,
                    NewElo = u.NewElo,
                    Change = u.EloChange,
                }).ToList(),
            },
        };

        await this.workflowEngineClient.NotifyEloUpdatedAsync(notifyEloUpdateReq);
        return eloUpdateResp;
    }

    /// <inheritdoc />
    public async Task<EloHistoryResponse> GetEloHistoryAsync(Guid userId)
    {
        var user = await this.userRepo.GetUserByIdAsync(userId, false, false, u => u.Statistics!, u => u.EloHistories);
        if (user == null)
        {
            this.logger.LogWarning($"Getting professional status failed because User with ID {userId} not found.");
            throw new KeyNotFoundException("User not found");
        }

        var stats = user.Statistics;
        var eloHistories = user.EloHistories.OrderBy(eh => eh.ChangedAt).ToList();
        var currentElo = stats?.CurrentElo ?? 0;
        var initialElo = eloHistories.FirstOrDefault()?.OldElo ?? 1200;
        var peakElo = stats?.PeakElo ?? currentElo;
        var gamesPlayed = stats?.GamesPlayed ?? eloHistories.Count;

        var eloTrend7 = this.GetEloTrend(eloHistories, 7);
        var eloTrend30 = this.GetEloTrend(eloHistories, 30);
        var winRate = this.GetWinRate(eloHistories);
        var avgOpponentElo = this.GetAverageOpponentElo(eloHistories);

        var historyList = eloHistories.Select(eh => new EloEntryDto
        {
            Date = eh.ChangedAt,
            OldElo = eh.OldElo,
            NewElo = eh.NewElo,
            Outcome = eh.Outcome, // e.g. "win", "loss"
            JobId = eh.JobId,
        }).ToList();

        var response = new EloHistoryResponse
        {
            UserId = user.Id,
            CurrentElo = currentElo,
            PeakElo = peakElo,
            InitialElo = initialElo,
            GamesPlayed = gamesPlayed,
            EloHistory = historyList,
            Trends = new EloTrendsDto
            {
                Last7Days = eloTrend7,
                Last30Days = eloTrend30,
                WinRate = winRate,
                AverageOpponentElo = avgOpponentElo,
            },
        };

        return response;
    }

    /// <inheritdoc />
    public async Task<string> GetEloTrend(Guid userId, int days)
    {
        var cutOffDate = DateTime.UtcNow.AddDays(-days);

        var recentHistory = await this.repo.FindAsync(
            eh => eh.UserId == userId && eh.ChangedAt >= cutOffDate);

        if (recentHistory == null || recentHistory.Count < 2)
        {
            return $"0_over_{days}_days";
        }

        var earliest = recentHistory.OrderBy(eh => eh.ChangedAt).First().NewElo;
        var latest = recentHistory.OrderByDescending(eh => eh.ChangedAt).First().NewElo;

        var diff = latest - earliest;
        var sign = diff >= 0 ? "+" : "-";

        return $"{sign}{Math.Abs(diff)}_over_{days}_days";
    }

    /// <inheritdoc />
    public string GetEloTrend(List<EloHistory> eloHistories, int days)
    {
        var cutOffDate = DateTime.UtcNow.AddDays(-days);

        var recentHistory = eloHistories.Where(eh => eh.ChangedAt >= cutOffDate).ToList();

        if (recentHistory == null || recentHistory.Count == 0)
        {
            return $"0_over_{days}_days";
        }

        var earliest = recentHistory.OrderBy(eh => eh.ChangedAt).First().OldElo;
        var latest = recentHistory.OrderByDescending(eh => eh.ChangedAt).First().NewElo;

        var diff = latest - earliest;
        var sign = diff >= 0 ? "+" : "-";

        return $"{sign}{Math.Abs(diff)}_over_{days}_days";
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, string>> BulkEloTrendAsync(List<Guid> userIds, int days)
    {
        var cutOffDate = DateTime.UtcNow.AddDays(-days);

        var recentHistories = await this.repo.FindAsync(
            eh => userIds.Contains(eh.UserId) && eh.ChangedAt >= cutOffDate);

        var grouped = recentHistories
            .GroupBy(eh => eh.UserId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var ordered = g.OrderBy(e => e.ChangedAt).ToList();
                    if (ordered.Count < 2)
                    {
                        return $"0_over_{days}_days";
                    }

                    var earliest = ordered.First().NewElo;
                    var latest = ordered.Last().NewElo;
                    var diff = latest - earliest;
                    var sign = diff >= 0 ? "+" : "-";
                    return $"{sign}{Math.Abs(diff)}_over_{days}_days";
                });

        // Handle users with no history
        foreach (var userId in userIds)
        {
            if (!grouped.ContainsKey(userId))
            {
                grouped[userId] = $"0_over_{days}_days";
            }
        }

        return grouped;
    }

    /// <inheritdoc />
    public double GetWinRate(List<EloHistory> eloHistories, int? days = null)
    {
        if (days.HasValue)
        {
            var cutOffDate = DateTime.UtcNow.AddDays(-days.Value);
            eloHistories = eloHistories.Where(eh => eh.ChangedAt >= cutOffDate).ToList();
        }

        int total = eloHistories.Count;
        if (total == 0)
        {
            return 0.00;
        }

        int wins = eloHistories.Count(eh => eh.Outcome == "win");
        double winRate = (double)wins / total * 100;
        return winRate;
    }

    /// <inheritdoc />
    public double GetAverageOpponentElo(List<EloHistory> eloHistories, int? days = null)
    {
        if (days.HasValue)
        {
            var cutOffDate = DateTime.UtcNow.AddDays(-days.Value);
            eloHistories = eloHistories.Where(eh => eh.ChangedAt >= cutOffDate).ToList();
        }

        var validEloEntries = eloHistories
            .Select(eh => eh.OpponentElo)
            .ToList();

        if (validEloEntries.Count == 0)
        {
            return 0;
        }

        return validEloEntries.Average();
    }

    /// <inheritdoc />
    public async Task<ThreeWayEloUpdateResponse> ResolveThreeWay(ThreeWayEloUpdateRequest twuReq)
    {
        // Validate input count
        if (twuReq.ThreeWayEloChanges == null || twuReq.ThreeWayEloChanges.Count != 3)
        {
            this.logger.LogWarning($"Exactly 3 Elo changes required for three-way resolution. Found: {twuReq.ThreeWayEloChanges?.Count}");
            throw new ArgumentException("Exactly 3 Elo changes required for three-way resolution.");
        }

        // Validate roles exist exactly once
        var roles = twuReq.ThreeWayEloChanges.Select(c => c.Role).ToList();

        var requiredRoles = new[]
        {
            ThreeWayTranscriberRoleType.OriginalTranscriber1.ToDisplayName(),
            ThreeWayTranscriberRoleType.OriginalTranscriber2.ToDisplayName(),
            ThreeWayTranscriberRoleType.TiebreakerTranscriber.ToDisplayName(),
        };

        foreach (var role in requiredRoles)
        {
            var roleCount = roles.Count(r => r == role);
            if (roleCount != 1)
            {
                this.logger.LogWarning($"Role '{role}' must appear exactly once in threeWayEloChanges. Found: {roleCount}");
                throw new ArgumentException($"Role '{role}' must appear exactly once in threeWayEloChanges.");
            }
        }

        var userIds = twuReq.ThreeWayEloChanges.Select(t => t.TranscriberId).Distinct().ToList();
        var userStatsDb = await this.userStatRepo.GetByUserIdsAsync(userIds, trackChanges: true);

        if (userStatsDb == null || userStatsDb.Count != userIds.Count)
        {
            this.logger.LogWarning($"Missing statistics for one or more transcribers. Expected: {userIds.Count}, Found: {userStatsDb?.Count}");
            throw new ArgumentException("Missing statistics for one or more transcribers.");
        }

        // Find tiebreaker transcriber change
        var tiebreakerChange = twuReq.ThreeWayEloChanges
            .First(c => c.Role == ThreeWayTranscriberRoleType.TiebreakerTranscriber.ToDisplayName());

        var eloHistories = new List<EloHistory>();
        var jobCompletions = new List<JobCompletion>();
        var updateResults = new List<EloUpdateResult>();
        var userNotifications = new List<UserNotification>();

        // We'll add tiebreaker's eloChange to original transcriber with minority outcome
        foreach (var change in twuReq.ThreeWayEloChanges)
        {
            var utcNow = DateTime.UtcNow;

            if (!EnumDisplayHelper.TryParseDisplayName(change.Role, out ThreeWayTranscriberRoleType roleEnum))
            {
                this.logger.LogWarning($"Invalid user role provided. Provided: {change.Role}");
                throw new ArgumentException($"Invalid user role provided");
            }

            if (!EnumDisplayHelper.TryParseDisplayName(change.Outcome, out GameOutcomeType outcomeEnum))
            {
                this.logger.LogWarning($"Invalid game outcome provided. Provided: {change.Outcome}");
                throw new ArgumentException($"Invalid game outcome provided");
            }

            var stats = userStatsDb.FirstOrDefault(u => u.UserId == change.TranscriberId)
                ?? throw new KeyNotFoundException($"User stats not found for {change.TranscriberId}");

            var bonus = change.TiebreakerBonus?.BonusAmount ?? 0;
            var eloChangeAdjusted = change.EloChange + bonus;

            var oldEloVal = stats.CurrentElo;
            var newElo = oldEloVal + eloChangeAdjusted;

            stats.CurrentElo = newElo;
            stats.PeakElo = Math.Max(stats.PeakElo, newElo);
            stats.GamesPlayed++;
            stats.TotalJobs++;
            stats.LastCalculated = utcNow;
            stats.UpdatedAt = utcNow;

            var eloHistoryRecord = new EloHistory
            {
                UserId = stats.UserId,
                OldElo = oldEloVal,
                NewElo = newElo,
                Reason = "three_way_resolution",
                Outcome = change.Outcome,
                ComparisonId = twuReq.OriginalComparisonId,
                JobId = twuReq.WorkflowRequestId ?? string.Empty,
                ComparisonType = "three_way",
                KFactorUsed = this.CalculateKFactor(stats.GamesPlayed),
                ChangedAt = utcNow,
            };
            eloHistories.Add(eloHistoryRecord);

            var jobCompletion = new JobCompletion
            {
                UserId = stats.UserId,
                JobId = twuReq.WorkflowRequestId ?? string.Empty,
                Outcome = change.Outcome,
                EloChange = change.EloChange,
                ComparisonId = twuReq.OriginalComparisonId,
                CreatedAt = utcNow,
                CompletedAt = utcNow,
            };
            jobCompletions.Add(jobCompletion);

            updateResults.Add(new EloUpdateResult
            {
                UserId = stats.UserId,
                NewElo = newElo,
                Change = eloChangeAdjusted,
            });

            userNotifications.Add(new UserNotification
            {
                UserId = stats.UserId,
                NotificationType = eloChangeAdjusted > 0 ? "elo_increase" : "elo_decrease",
                Message = eloChangeAdjusted > 0
                    ? $"Great job! Your Elo rating increased by {eloChangeAdjusted} points to {newElo}."
                    : $"Your Elo rating decreased by {Math.Abs(eloChangeAdjusted)} points to {newElo}. Review the feedback for improvement tips.",
            });

            // For redis update
            var cutOffDate = DateTime.UtcNow.AddDays(-7);
            var recentHistory = await this.repo.FindAsync(
                eh => eh.UserId == change.TranscriberId && eh.ChangedAt >= cutOffDate);
            recentHistory.Add(eloHistoryRecord);
            await this.redisService.SetUserEloAsync(change.TranscriberId, new UserEloRedisDto
            {
                CurrentElo = newElo,
                PeakElo = stats.PeakElo,
                GamesPlayed = stats.GamesPlayed,
                RecentTrend = this.GetEloTrend(recentHistory, 7),
                LastJobCompleted = jobCompletion.CompletedAt,
            });
        }

        await this.unitOfWork.EloHistories.AddRangeAsync(eloHistories);
        await this.unitOfWork.JobCompletions.AddRangeAsync(jobCompletions);
        await this.unitOfWork.SaveChangesAsync();

        var notifyReq = new EloUpdateNotificationRequest
        {
            UpdateId = twuReq.WorkflowRequestId ?? string.Empty,
            EventType = WorkflowEventType.EloUpdated.ToDisplayName(),
            EventData = new EloUpdateEventData
            {
                ComparisonId = twuReq.OriginalComparisonId,
                UsersUpdated = eloHistories.Count,
                UpdateResults = updateResults,
            },
        };

        await this.workflowEngineClient.NotifyEloUpdatedAsync(notifyReq);

        return new ThreeWayEloUpdateResponse
        {
            EloUpdateConfirmed = true,
            UpdatesApplied = eloHistories.Count,
            Timestamp = DateTime.UtcNow,
            UserNotifications = userNotifications,
        };
    }

    private int CalculateKFactor(int gamesPlayed)
    {
        return gamesPlayed switch
        {
            < 30 => this.eloKFactorNew,
            < 100 => this.eloKFactorEstablished,
            _ => this.eloKFactorExpert
        };
    }
}

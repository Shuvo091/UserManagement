// <copyright file="EloService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
        this.eloKFactorNew = appContantOptions.Value.EloKFactorNew;
        this.eloKFactorEstablished = appContantOptions.Value.EloKFactorEstablished;
        this.eloKFactorExpert = appContantOptions.Value.EloKFactorExpert;
        this.workflowEngineClient = workflowEngineClient;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<EloUpdateResponse> ApplyEloUpdatesAsync(EloUpdateRequest request)
    {
        if (request == null)
        {
            this.logger.LogInformation("Request cannot be null.");
            throw new ArgumentNullException(nameof(request));
        }

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

        this.logger.LogInformation("Applying Elo updates. WorkflowRequestId: {WorkflowRequestId}, Transcribers: {UserIds}", request.WorkflowRequestId, userIds);

        var userStatisticsDb = await this.unitOfWork.UserStatistics
            .GetByUserIdsAsync(userIds, trackChanges: true);

        if (userStatisticsDb == null || userStatisticsDb.Count != userIds.Count)
        {
            this.logger.LogWarning("Missing UserStatistics for some transcribers. Expected: {ExpectedCount}, Found: {FoundCount}", userIds.Count, userStatisticsDb?.Count ?? 0);
            throw new ArgumentException("Missing UserStatistics for some transcribers.");
        }

        if (request.RecommendedEloChanges.Count > 2)
        {
            this.logger.LogWarning("Unexpected number of Elo changes found: {Count}. Expected 2.", request.RecommendedEloChanges.Count);
            throw new ArgumentException("Unexpected number of Elo change requests found.");
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

            this.logger.LogInformation("Updating Elo for UserId: {UserId}, OldElo: {OldElo}, Change: {Change}, NewElo: {NewElo}", eloChange.TranscriberId, eloChange.OldElo, eloChange.RecommendedChange, newElo);

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

            recentHistory ??= new List<EloHistory>();
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

        this.logger.LogInformation("Persisting Elo history records and job completions. WorkflowRequestId: {WorkflowRequestId}", request.WorkflowRequestId);
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

        // Try-catch only for external systems
        try
        {
            await this.eventBus.PublishAsync(cloudEvent, TopicConstant.UserEloUpdated);
            this.logger.LogInformation("CloudEvent publish successful. WorkflowRequestId: {WorkflowRequestId}", request.WorkflowRequestId);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "CloudEvent publish failed. WorkflowRequestId: {WorkflowRequestId}", request.WorkflowRequestId);
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

        try
        {
            await this.workflowEngineClient.NotifyEloUpdatedAsync(notifyEloUpdateReq);
            this.logger.LogInformation("Workflow engine notified successfully. WorkflowRequestId: {WorkflowRequestId}", request.WorkflowRequestId);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to notify workflow engine. WorkflowRequestId: {WorkflowRequestId}", request.WorkflowRequestId);
        }

        return eloUpdateResp;
    }

    /// <inheritdoc />
    public async Task<EloHistoryResponse> GetEloHistoryAsync(Guid userId)
    {
        this.logger.LogInformation("Fetching Elo history. UserId: {UserId}", userId);

        var user = await this.userRepo.GetUserByIdAsync(userId, false, false, u => u.Statistics!, u => u.EloHistories);
        if (user == null)
        {
            this.logger.LogWarning("User not found while fetching Elo history. UserId: {UserId}", userId);
            throw new KeyNotFoundException($"User not found with ID {userId}");
        }

        var stats = user.Statistics;
        var eloHistories = user.EloHistories.OrderBy(eh => eh.ChangedAt).ToList();
        var currentElo = stats?.CurrentElo ?? 0;
        var initialElo = eloHistories.FirstOrDefault()?.OldElo ?? 1200;
        var peakElo = stats?.PeakElo ?? currentElo;
        var gamesPlayed = stats?.GamesPlayed ?? eloHistories.Count;

        this.logger.LogInformation("Elo stats calculated. UserId: {UserId}, CurrentElo: {CurrentElo}, PeakElo: {PeakElo}, GamesPlayed: {GamesPlayed}", userId, currentElo, peakElo, gamesPlayed);

        var eloTrend7 = this.GetEloTrend(eloHistories, 7);
        var eloTrend30 = this.GetEloTrend(eloHistories, 30);
        var winRate = this.GetWinRate(eloHistories);
        var avgOpponentElo = this.GetAverageOpponentElo(eloHistories);

        var historyList = eloHistories.Select(eh => new EloEntryDto
        {
            Date = eh.ChangedAt,
            OldElo = eh.OldElo,
            NewElo = eh.NewElo,
            Outcome = eh.Outcome,
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

        this.logger.LogInformation("Elo history response prepared. UserId: {UserId}, HistoryCount: {HistoryCount}", userId, historyList.Count);
        return response;
    }

    /// <inheritdoc />
    public async Task<string> GetEloTrend(Guid userId, int days)
    {
        var cutOffDate = DateTime.UtcNow.AddDays(-days);
        this.logger.LogInformation("Calculating Elo trend for UserId: {UserId} over {Days} days", userId, days);

        var recentHistory = await this.repo.FindAsync(
            eh => eh.UserId == userId && eh.ChangedAt >= cutOffDate);

        if (recentHistory == null || recentHistory.Count < 2)
        {
            this.logger.LogInformation("Not enough Elo history to calculate trend. UserId: {UserId}, EntriesFound: {Count}", userId, recentHistory?.Count ?? 0);
            return $"0_over_{days}_days";
        }

        var earliest = recentHistory.OrderBy(eh => eh.ChangedAt).First().NewElo;
        var latest = recentHistory.OrderByDescending(eh => eh.ChangedAt).First().NewElo;
        var diff = latest - earliest;
        var sign = diff >= 0 ? "+" : "-";

        var trend = $"{sign}{Math.Abs(diff)}_over_{days}_days";
        this.logger.LogInformation("Elo trend calculated for UserId: {UserId}: {Trend}", userId, trend);

        return trend;
    }

    /// <inheritdoc />
    public string GetEloTrend(List<EloHistory> eloHistories, int days)
    {
        if (eloHistories == null)
        {
            this.logger.LogInformation("Elo histories cannot be null.");
            throw new ArgumentNullException(nameof(eloHistories));
        }

        if (days <= 0)
        {
            this.logger.LogInformation("Days must be greater than zero. Got {days}", days);
            throw new ArgumentOutOfRangeException(nameof(days), "Days must be greater than zero.");
        }

        var cutOffDate = DateTime.UtcNow.AddDays(-days);
        this.logger.LogInformation("Calculating Elo trend from in-memory history over {Days} days, Entries: {Count}", days, eloHistories.Count);

        var recentHistory = eloHistories.Where(eh => eh.ChangedAt >= cutOffDate).ToList();
        if (recentHistory.Count == 0)
        {
            this.logger.LogInformation("No Elo history found in the given period. Returning default trend.");
            return $"0_over_{days}_days";
        }

        var earliest = recentHistory.OrderBy(eh => eh.ChangedAt).First().OldElo;
        var latest = recentHistory.OrderByDescending(eh => eh.ChangedAt).First().NewElo;
        var diff = latest - earliest;
        var sign = diff >= 0 ? "+" : "-";
        var trend = $"{sign}{Math.Abs(diff)}_over_{days}_days";
        this.logger.LogInformation("Elo trend calculated from in-memory history: {Trend}", trend);
        return trend;
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, string>> BulkEloTrendAsync(List<Guid> userIds, int days)
    {
        if (userIds == null || userIds.Count == 0)
        {
            this.logger.LogInformation("UserIds cannot be null.");
            throw new ArgumentException("UserIds cannot be null or empty.", nameof(userIds));
        }

        if (days <= 0)
        {
            this.logger.LogInformation("Days must be greater than zero. Got {days}", days);
            throw new ArgumentOutOfRangeException(nameof(days), "Days must be greater than zero.");
        }

        this.logger.LogInformation("Calculating bulk Elo trend for {Count} users over {Days} days", userIds.Count, days);
        var cutOffDate = DateTime.UtcNow.AddDays(-days);

        var recentHistories = await this.repo.FindAsync(eh => userIds.Contains(eh.UserId) && eh.ChangedAt >= cutOffDate);
        if (recentHistories == null)
        {
            recentHistories = new List<EloHistory>();
        }

        var grouped = recentHistories.GroupBy(eh => eh.UserId).ToDictionary(g => g.Key, g =>
        {
            var ordered = g.OrderBy(e => e.ChangedAt).ToList();
            var earliest = ordered.First().OldElo;
            var latest = ordered.Last().NewElo;
            var diff = latest - earliest;
            var sign = diff >= 0 ? "+" : "-";
            var trend = $"{sign}{Math.Abs(diff)}_over_{days}_days";
            this.logger.LogInformation("Calculated Elo trend for UserId: {UserId}: {Trend}", g.Key, trend);
            return trend;
        });

        foreach (var userId in userIds)
        {
            if (!grouped.ContainsKey(userId))
            {
                this.logger.LogInformation("No Elo history found for UserId: {UserId}. Returning default trend.", userId);
                grouped[userId] = $"0_over_{days}_days";
            }
        }

        return grouped;
    }

    /// <inheritdoc />
    public double GetWinRate(List<EloHistory> eloHistories, int? days = null)
    {
        if (eloHistories == null)
        {
            this.logger.LogInformation("Elo histories cannot be null.");
            throw new ArgumentNullException(nameof(eloHistories));
        }

        if (days.HasValue && days.Value <= 0)
        {
            this.logger.LogInformation("Days must be greater than zero. Got {days}", days);
            throw new ArgumentOutOfRangeException(nameof(days), "Days must be greater than zero.");
        }

        if (days.HasValue)
        {
            var cutOffDate = DateTime.UtcNow.AddDays(-days.Value);
            this.logger.LogInformation("Filtering Elo history for win rate over last {Days} days. Original entries: {Count}", days.Value, eloHistories.Count);
            eloHistories = eloHistories.Where(eh => eh.ChangedAt >= cutOffDate).ToList();
        }

        int total = eloHistories.Count;
        if (total == 0)
        {
            this.logger.LogInformation("No Elo history available. Win rate is 0%");
            return 0.00;
        }

        int wins = eloHistories.Count(eh => eh.Outcome == "win");
        double winRate = (double)wins / total * 100;
        this.logger.LogInformation("Calculated win rate: {WinRate}% (Wins: {Wins}, Total: {Total})", winRate, wins, total);
        return winRate;
    }

    /// <inheritdoc />
    public double GetAverageOpponentElo(List<EloHistory> eloHistories, int? days = null)
    {
        if (eloHistories == null)
        {
            this.logger.LogInformation("Elo histories cannot be null.");
            throw new ArgumentNullException(nameof(eloHistories));
        }

        if (days.HasValue && days.Value <= 0)
        {
            this.logger.LogInformation("Days must be greater than zero. Found : {days}", days);
            throw new ArgumentOutOfRangeException(nameof(days), "Days must be greater than zero.");
        }

        if (eloHistories.Count == 0)
        {
            this.logger.LogInformation("No Elo history provided. Returning 0.");
            return 0;
        }

        if (days.HasValue)
        {
            var cutOffDate = DateTime.UtcNow.AddDays(-days.Value);
            this.logger.LogInformation("Filtering Elo history for average opponent Elo over last {Days} days. Original entries: {Count}", days.Value, eloHistories.Count);
            eloHistories = eloHistories.Where(eh => eh.ChangedAt >= cutOffDate).ToList();
        }

        var validEloEntries = eloHistories.Select(eh => eh.OpponentElo).ToList();
        if (validEloEntries.Count == 0)
        {
            this.logger.LogInformation("No valid opponent Elo entries. Returning 0.");
            return 0;
        }

        var average = validEloEntries.Average();
        this.logger.LogInformation("Calculated average opponent Elo: {Average}", average);
        return average;
    }

    /// <inheritdoc />
    public async Task<ThreeWayEloUpdateResponse> ResolveThreeWay(ThreeWayEloUpdateRequest twuReq)
    {
        if (twuReq == null)
        {
            this.logger.LogWarning("Request cannot be null or empty.");
            throw new ArgumentNullException(nameof(twuReq));
        }

        if (string.IsNullOrEmpty(twuReq.WorkflowRequestId))
        {
            this.logger.LogWarning("WorkflowRequestId cannot be null or empty. WorkflowRequestId: {WorkflowRequestId}", nameof(twuReq.WorkflowRequestId));
            throw new ArgumentException("WorkflowRequestId cannot be null or empty.", nameof(twuReq.WorkflowRequestId));
        }

        this.logger.LogInformation("Starting three-way Elo resolution for WorkflowRequestId: {WorkflowRequestId}", twuReq.WorkflowRequestId);

        if (twuReq.ThreeWayEloChanges == null || twuReq.ThreeWayEloChanges.Count != 3)
        {
            this.logger.LogWarning("Exactly 3 Elo changes required. Found: {Count}", twuReq.ThreeWayEloChanges?.Count);
            throw new ArgumentException("Exactly 3 Elo changes required for three-way resolution.");
        }

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
                this.logger.LogWarning("Role '{Role}' must appear exactly once. Found: {Count}", role, roleCount);
                throw new ArgumentException($"Role '{role}' must appear exactly once in threeWayEloChanges.");
            }
        }

        var userIds = twuReq.ThreeWayEloChanges.Select(t => t.TranscriberId).Distinct().ToList();
        var userStatsDb = await this.userStatRepo.GetByUserIdsAsync(userIds, trackChanges: true);
        if (userStatsDb == null || userStatsDb.Count != userIds.Count)
        {
            this.logger.LogWarning("Missing statistics for transcribers. Expected: {Expected}, Found: {Found}", userIds.Count, userStatsDb?.Count);
            throw new ArgumentException("Missing statistics for one or more transcribers.");
        }

        this.logger.LogInformation("Processing Elo updates for users: {UserIds}", string.Join(", ", userIds));

        var eloHistories = new List<EloHistory>();
        var jobCompletions = new List<JobCompletion>();
        var updateResults = new List<EloUpdateResult>();
        var userNotifications = new List<UserNotification>();
        var cutOffDate = DateTime.UtcNow.AddDays(-7);

        foreach (var change in twuReq.ThreeWayEloChanges)
        {
            var utcNow = DateTime.UtcNow;

            if (!EnumDisplayHelper.TryParseDisplayName(change.Role, out ThreeWayTranscriberRoleType roleEnum))
            {
                this.logger.LogWarning("Invalid user role provided: {Role}", change.Role);
                throw new ArgumentException("Invalid user role provided");
            }

            if (!EnumDisplayHelper.TryParseDisplayName(change.Outcome, out GameOutcomeType outcomeEnum))
            {
                this.logger.LogWarning("Invalid game outcome provided: {Outcome}", change.Outcome);
                throw new ArgumentException("Invalid game outcome provided");
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

            this.logger.LogInformation("User {UserId}: Elo {OldElo} -> {NewElo} (Change: {Change})", stats.UserId, oldEloVal, newElo, eloChangeAdjusted);

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
                    : $"Your Elo rating decreased by {Math.Abs(eloChangeAdjusted)} points to {newElo}. Review feedback for improvement.",
            });

            var recentHistory = await this.repo.FindAsync(eh => eh.UserId == change.TranscriberId && eh.ChangedAt >= cutOffDate);
            if (recentHistory == null)
            {
                recentHistory = new List<EloHistory>();
            }

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

        var cloudEvent = new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri($"{TopicConstant.UserEloUpdated}:{twuReq.WorkflowRequestId}"),
            Type = TopicConstant.UserEloUpdated,
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json",
            Data = new { RequestId = twuReq.WorkflowRequestId, Message = "Users Elo Updated." },
        };
        try
        {
            await this.eventBus.PublishAsync(cloudEvent, TopicConstant.UserEloUpdated);
            this.logger.LogInformation("CloudEvent published successfully for WorkflowRequestId: {WorkflowRequestId}", twuReq.WorkflowRequestId);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to publish CloudEvent for WorkflowRequestId: {WorkflowRequestId}", twuReq.WorkflowRequestId);
        }

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

        try
        {
            await this.workflowEngineClient.NotifyEloUpdatedAsync(notifyReq);
            this.logger.LogInformation("Workflow engine notified for Elo updates. WorkflowRequestId: {WorkflowRequestId}", twuReq.WorkflowRequestId);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to notify workflow engine for Elo update. WorkflowRequestId: {WorkflowRequestId}", twuReq.WorkflowRequestId);
        }

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

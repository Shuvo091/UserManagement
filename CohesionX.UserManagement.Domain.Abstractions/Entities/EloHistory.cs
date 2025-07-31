// <copyright file="EloHistory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CohesionX.UserManagement.Database.Abstractions.Entities;

/// <summary>
/// Represents a historical record of Elo rating changes for a user
/// as a result of a comparison or evaluation within the system.
/// </summary>
public class EloHistory : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the user whose Elo was updated.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the Elo rating before the change.
    /// </summary>
    public int OldElo { get; set; }

    /// <summary>
    /// Gets or sets the Elo rating after the change.
    /// </summary>
    public int NewElo { get; set; }

    /// <summary>
    /// Gets or sets the Elo rating of the opponent or peer user during comparison.
    /// </summary>
    public int OpponentElo { get; set; }

    /// <summary>
    /// Gets or sets the reason for the Elo change (e.g., "QA comparison", "Manual adjustment").
    /// </summary>
    public string Reason { get; set; } = default!;

    /// <summary>
    /// Gets or sets the unique identifier of the comparison session
    /// or evaluation instance that triggered the Elo update.
    /// </summary>
    public Guid ComparisonId { get; set; }

    /// <summary>
    /// Gets or sets the job ID associated with the Elo change context.
    /// </summary>
    public string JobId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the outcome of the comparison (e.g., "win", "loss", "draw").
    /// </summary>
    public string Outcome { get; set; } = default!;

    /// <summary>
    /// Gets or sets the type of comparison (e.g., "QA", "PeerReview").
    /// </summary>
    public string ComparisonType { get; set; } = default!;

    /// <summary>
    /// Gets or sets the K-factor used in Elo calculation,
    /// which influences the magnitude of the rating change.
    /// </summary>
    public int KFactorUsed { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the Elo change occurred.
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Gets or sets navigation property to the user whose Elo was changed.
    /// </summary>
    public User User { get; set; } = default!;

    /// <summary>
    /// Gets or sets navigation property to the comparison peer user.
    /// Note: This is often another user who was involved in the evaluation.
    /// </summary>
    public User Comparison { get; set; } = default!;
}

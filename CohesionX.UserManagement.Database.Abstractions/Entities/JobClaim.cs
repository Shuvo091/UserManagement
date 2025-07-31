// <copyright file="JobClaim.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CohesionX.UserManagement.Database.Abstractions.Entities;

/// <summary>
/// Represents a record of a job claimed by a user for transcription or processing,
/// including timestamps and status metadata.
/// </summary>
public class JobClaim : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the user who claimed the job.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the job that was claimed.
    /// </summary>
    public string JobId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the timestamp (UTC) when the job was claimed by the user.
    /// </summary>
    public DateTime ClaimedAt { get; set; }

    /// <summary>
    /// Gets or sets the expiration timestamp (UTC) for the job's book-out window.
    /// After this time, the job may be reclaimed or reassigned.
    /// </summary>
    public DateTime BookOutExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the status of the job claim
    /// (e.g., "claimed", "completed", "expired", "released").
    /// </summary>
    public string Status { get; set; } = default!;

    /// <summary>
    /// Gets or sets the timestamp (UTC) when this job claim record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets navigation property to the user who claimed the job.
    /// </summary>
    public User User { get; set; } = default!;
}

// <copyright file="User.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedLibrary.AppEnums;

namespace CohesionX.UserManagement.Database.Abstractions.Entities;

/// <summary>
/// Represents a user within the User Management domain,
/// including identity, contact details, status, and related activity.
/// </summary>
public class User : BaseEntity
{
    // ───────────── Core Properties ─────────────

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string FirstName { get; set; } = default!;

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string LastName { get; set; } = default!;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = default!;

    /// <summary>
    /// Gets or sets the username used for authentication or display.
    /// </summary>
    public string UserName { get; set; } = default!;

    /// <summary>
    /// Gets or sets the hashed representation of the user's password.
    /// </summary>
    public string PasswordHash { get; set; } = default!;

    /// <summary>
    /// Gets or sets the user's phone number (optional).
    /// </summary>
    public string? Phone { get; set; } = default!;

    /// <summary>
    /// Gets or sets the national identification number (optional).
    /// </summary>
    public string? IdNumber { get; set; } = default!;

    /// <summary>
    /// Gets or sets the current verification status of the user.
    /// Defaults to <c>PendingVerification</c>.
    /// </summary>
    public string Status { get; set; } = UserStatusType.PendingVerification.ToDisplayName();

    /// <summary>
    /// Gets or sets the role of the user (e.g., Transcriber, QA, Admin).
    /// Defaults to <c>Transcriber</c>.
    /// </summary>
    public string Role { get; set; } = UserRoleType.Transcriber.ToDisplayName();

    /// <summary>
    /// Gets or sets a value indicating whether the user is classified as a professional.
    /// </summary>
    public bool IsProfessional { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the user record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when the user record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ───────────── Navigation Properties ─────────────

    /// <summary>
    /// Gets or sets the collection of dialects associated with the user.
    /// </summary>
    public ICollection<UserDialect> Dialects { get; set; } = [];

    /// <summary>
    /// Gets or sets the statistical metadata for the user (e.g., Elo rating).
    /// </summary>
    public UserStatistics? Statistics { get; set; }

    /// <summary>
    /// Gets or sets the statistical metadata for the user (e.g., Elo rating).
    /// </summary>
    public UserVerificationRequirement? UserVerificationRequirement { get; set; }

    /// <summary>
    /// Gets or sets the historical record of Elo rating changes for the user.
    /// </summary>
    public ICollection<EloHistory> EloHistories { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of jobs the user has completed.
    /// </summary>
    public ICollection<JobCompletion> JobCompletions { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of jobs the user has claimed.
    /// </summary>
    public ICollection<JobClaim> JobClaims { get; set; } = [];

    /// <summary>
    /// Gets or sets the audit logs associated with user actions.
    /// </summary>
    public ICollection<AuditLog> AuditLogs { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of verification records associated with the user.
    /// </summary>
    public ICollection<VerificationRecord> VerificationRecords { get; set; } = [];
}

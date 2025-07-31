// <copyright file="UserDialect.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CohesionX.UserManagement.Database.Abstractions.Entities;

/// <summary>
/// Represents a dialect spoken by a user, including their proficiency and whether it's their primary dialect.
/// </summary>
public class UserDialect : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the user associated with this dialect.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the dialect (e.g., "Zulu", "Xhosa", "Afrikaans").
    /// </summary>
    public string Dialect { get; set; } = default!;

    /// <summary>
    /// Gets or sets the user's proficiency level in the dialect
    /// (e.g., "Native", "Fluent", "Intermediate").
    /// </summary>
    public string ProficiencyLevel { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether this dialect is the user's primary spoken dialect.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Gets or sets the timestamp (UTC) when this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets navigation property to the user who speaks this dialect.
    /// </summary>
    public User User { get; set; } = default!;
}

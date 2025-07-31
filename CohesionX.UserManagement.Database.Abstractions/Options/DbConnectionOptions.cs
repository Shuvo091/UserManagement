// <copyright file="DbConnectionOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CohesionX.UserManagement.Database.Abstractions.Options;

/// <summary>
/// Represents options for database connection settings.
/// </summary>
public class DbConnectionOptions
{
    /// <summary>
    /// Gets or sets the database connection string, which includes the secrets for connecting to the database.
    /// </summary>
    public string DbSecrets { get; set; } = default!;
}

// <copyright file="DbSeederOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CohesionX.UserManagement.Database.Abstractions.Options;

/// <summary>
/// Db seeder option model.
/// </summary>
public class DbSeederOptions
{
    /// <summary>
    /// Gets or sets path to seeder file.
    /// </summary>
    public string SeedFilePath { get; set; } = string.Empty;
}

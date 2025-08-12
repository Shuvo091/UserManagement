namespace CohesionX.UserManagement.Abstractions.DTOs.Options;

/// <summary>
/// Options for seed file paths used in database initialization.
/// </summary>
public class SeedFilePathsOptions
{
    /// <summary>
    /// Gets or sets the path segments to the AdminConfig.json file.
    /// </summary>
    required public string[] AdminAndQaUserSeederFilePath { get; set; }
}
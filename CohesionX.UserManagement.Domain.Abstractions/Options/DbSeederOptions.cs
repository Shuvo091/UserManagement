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

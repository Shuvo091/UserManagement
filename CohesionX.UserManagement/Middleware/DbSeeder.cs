using System.Text.Json;
using CohesionX.UserManagement.Domain.Entities;
using CohesionX.UserManagement.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Middleware;

/// <summary>
/// Provides database seeding operations for the User Management module.
/// </summary>
public static class DbSeeder
{
	/// <summary>
	/// Seeds the global user verification requirement into the database
	/// if none currently exist.
	/// </summary>
	/// <param name="context">The application database context.</param>
	/// <returns>A task that represents the asynchronous seeding operation.</returns>
	/// <exception cref="FileNotFoundException">Thrown when the seed JSON file cannot be found.</exception>
	/// <exception cref="InvalidOperationException">Thrown when seed data parsing fails.</exception>
	public static async Task SeedGlobalVerificationRequirementAsync(AppDbContext context)
	{
		// Check if any verification requirements already exist to avoid duplicate seeding
		if (await context.UserVerificationRequirements.AnyAsync())
			return; // Already seeded

		// Compose the path to the seed file relative to the application base directory
		var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DbSeeder.json");
		if (!File.Exists(jsonPath))
			throw new FileNotFoundException("Seed file not found", jsonPath);

		// Read JSON seed data asynchronously
		var json = await File.ReadAllTextAsync(jsonPath);

		// Deserialize JSON into UserVerificationRequirement model with case-insensitive property names
		var model = JsonSerializer.Deserialize<UserVerificationRequirement>(json, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		if (model is null)
			throw new InvalidOperationException("Failed to parse seed data.");

		// Assign a new unique ID to ensure a fresh database record
		model.Id = Guid.NewGuid();

		// Re-serialize the ValidationRules dictionary back to JSON for database storage
		model.ValidationRulesJson = JsonSerializer.Serialize(model.ValidationRules);

		// Add the new entity and save changes asynchronously
		context.UserVerificationRequirements.Add(model);
		await context.SaveChangesAsync();
	}
}

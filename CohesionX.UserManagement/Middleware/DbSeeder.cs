using System.Text.Json;
using CohesionX.UserManagement.Application.Models;
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
	/// <param name="dbSeederOptions">Options for DB seeder.</param>
	/// <returns>A task that represents the asynchronous seeding operation.</returns>
	/// <exception cref="FileNotFoundException">Thrown when the seed JSON file cannot be found.</exception>
	/// <exception cref="InvalidOperationException">Thrown when seed data parsing fails.</exception>
	public static async Task SeedGlobalVerificationRequirementAsync(AppDbContext context, DbSeederOptions dbSeederOptions)
	{
		if (await context.UserVerificationRequirements.AnyAsync())
		{
			return;
		}

		// Use configured path
		var relativePath = dbSeederOptions.SeedFilePath ?? "DbSeeder.json";
		var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

		if (!File.Exists(jsonPath))
		{
			throw new FileNotFoundException("Seed file not found", jsonPath);
		}

		var json = await File.ReadAllTextAsync(jsonPath);

		var model = JsonSerializer.Deserialize<UserVerificationRequirement>(json, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
		});

		if (model is null)
		{
			throw new InvalidOperationException("Failed to parse seed data.");
		}

		model.Id = Guid.NewGuid();
		model.ValidationRulesJson = JsonSerializer.Serialize(model.ValidationRules);

		context.UserVerificationRequirements.Add(model);
		await context.SaveChangesAsync();
	}
}

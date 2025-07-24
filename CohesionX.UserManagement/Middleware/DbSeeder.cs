using System.Text.Json;
using CohesionX.UserManagement.Domain.Entities;
using CohesionX.UserManagement.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Middleware;

public static class DbSeeder
{
	public static async Task SeedGlobalVerificationRequirementAsync(AppDbContext context)
	{
		if (await context.UserVerificationRequirements.AnyAsync())
			return; // Already seeded

		var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DbSeeder.json");
		if (!File.Exists(jsonPath))
			throw new FileNotFoundException("Seed file not found", jsonPath);

		var json = await File.ReadAllTextAsync(jsonPath);
		var model = JsonSerializer.Deserialize<UserVerificationRequirement>(json, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		if (model is null)
			throw new InvalidOperationException("Failed to parse seed data.");

		model.Id = Guid.NewGuid(); // ensure new ID
		model.ValidationRulesJson = JsonSerializer.Serialize(model.ValidationRules);

		context.UserVerificationRequirements.Add(model);
		await context.SaveChangesAsync();
	}
}

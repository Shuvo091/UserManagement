using CohesionX.UserManagement.Application.Models;
using CohesionX.UserManagement.Middleware;
using CohesionX.UserManagement.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Services;

/// <summary>
/// Migrates the DB for code first approach and then seeds data if necessary.
/// </summary>
public class MigrationAndSeedingService : IHostedService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly IConfiguration _configuration;

	/// <summary>
	/// Initializes a new instance of the <see cref="MigrationAndSeedingService"/> class.
	/// </summary>
	/// <param name="serviceProvider">Service provider.</param>
	/// <param name="configuration">For Dbseeder options from config.</param>
	public MigrationAndSeedingService(IServiceProvider serviceProvider, IConfiguration configuration)
	{
		_serviceProvider = serviceProvider;
		_configuration = configuration;
	}

	/// <summary>
	/// Starts the process of migrating the database and seeding initial data asynchronously.
	/// </summary>
	/// <param name="cancellationToken">optionally cancel.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		using var scope = _serviceProvider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		await context.Database.MigrateAsync(cancellationToken);

		// Read seed path from config
		var seedFilePath = _configuration["DbSeeder:SeedFilePath"];
		if (string.IsNullOrWhiteSpace(seedFilePath))
		{
			throw new InvalidOperationException("SeedFilePath not configured.");
		}

		DbSeederOptions dbSeederOptions = new DbSeederOptions
		{
			SeedFilePath = seedFilePath,
		};

		await DbSeeder.SeedGlobalVerificationRequirementAsync(context, dbSeederOptions);
	}

	/// <summary>
	/// Starts the process of migrating the database and seeding initial data asynchronously.
	/// </summary>
	/// <param name="cancellationToken">optionally cancel.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

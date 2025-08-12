using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Application.Services;
using CohesionX.UserManagement.Database;
using CohesionX.UserManagement.Initializers.TaskTracker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CohesionX.UserManagement.Initializers;

/// <summary>
/// Initializes seed data for the application database.
/// </summary>
public class SeedInitializer : IHostedService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IStartupTaskTracker startupTaskTracker;
    private readonly ILogger<SeedInitializer> logger;
    private readonly SeedFilePathsOptions seedFilePathsOptions;
    private readonly string basePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeedInitializer"/> class,  which is responsible for seeding
    /// data using the provided service provider.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve dependencies required for data seeding.</param>
    /// <param name="filePathsOptions">The <see cref="IOptions{SeedFilePathsOptions}"/> used to access file paths from configuration.</param>
    /// <param name="environment"><see cref="IWebHostEnvironment"/> variable.</param>
    /// <param name="startupTaskTracker">The startup task tracker.</param>
    /// <param name="logger">Logger.</param>
    public SeedInitializer(
        IServiceProvider serviceProvider,
        IOptions<SeedFilePathsOptions> filePathsOptions,
        IWebHostEnvironment environment,
        IStartupTaskTracker startupTaskTracker,
        ILogger<SeedInitializer> logger)
    {
        this.serviceProvider = serviceProvider;
        this.startupTaskTracker = startupTaskTracker;
        this.logger = logger;
        this.seedFilePathsOptions = filePathsOptions.Value;
        this.basePath = environment.ContentRootPath;
    }

    /// <summary>
    /// Starts the asynchronous initialization process.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the initialization process.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = this.serviceProvider.CreateScope();
        await this.InitializeAsync(scope.ServiceProvider);
    }

    /// <summary>
    /// Stops the asynchronous operation gracefully.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. If the token is triggered, the operation should stop promptly.</param>
    /// <returns>A completed <see cref="Task"/> that represents the asynchronous stop operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Initializes the database and seeds configuration data.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    private async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        await using var context = serviceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();

        var userSeederService = serviceProvider.GetRequiredService<AdminAndQaUserSeederService>();
        var userSeederFilePath = Path.Combine(this.basePath, Path.Combine(this.seedFilePathsOptions.AdminAndQaUserSeederFilePath));

        this.logger.LogInformation("Seeding Admin Configurations from file: {userSeederFilePath}", userSeederFilePath);

        await userSeederService.PopulateDatabaseAsync(userSeederFilePath);

        this.logger.LogInformation("Database seeding completed successfully.");

        this.startupTaskTracker.MarkCompleted();
    }
}
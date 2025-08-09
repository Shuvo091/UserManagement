using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CohesionX.UserManagement.Application.Extensions;

/// <summary>
/// Extension method for applying migrations.
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations on application startup.
    /// </summary>
    /// <typeparam name="TContext"> Type of context. </typeparam>
    /// <param name="app"> The app from program.cs. </param>
    /// <returns> Returns web application. </returns>
    public static WebApplication ApplyDatabaseMigrations<TContext>(this WebApplication app)
        where TContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        dbContext.Database.Migrate();

        return app;
    }
}
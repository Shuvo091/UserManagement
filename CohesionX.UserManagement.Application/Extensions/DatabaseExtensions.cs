using CohesionX.UserManagement.Database;
using CohesionX.UserManagement.Database.Abstractions.Contants;
using CohesionX.UserManagement.Database.Abstractions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CohesionX.UserManagement.Application.Extensions;

/// <summary>
/// Extension methods for configuring the database context in the application.
/// </summary>
public static class DatabaseExtensions
{
	/// <summary>
	/// Registers the application database context with the dependency injection container.
	/// </summary>
	/// <param name="services"> service to extend. </param>
	/// <param name="config"> base cofig. </param>
	/// <returns> returns augmented collection. </returns>
	public static IServiceCollection AddAppDbContext(this IServiceCollection services, IConfiguration config)
	{
		var dbOptions = config.GetSection("DB_CONNECTION_STRING").Get<DbConnectionOptions>()!;
		services.AddDbContext<AppDbContext>(options =>
		{
			options.UseNpgsql(dbOptions.DbSecrets, npgsql =>
			{
				npgsql.MigrationsHistoryTable("__EFMigrationsHistory", DbSchema.Default);
			});
		});
		return services;
	}
}

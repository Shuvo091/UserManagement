namespace CohesionX.UserManagement.Abstractions.DTOs.Options;

/// <summary>
/// Represents options for Redis cache configuration.
/// </summary>
public class RedisOptions
{
	/// <summary>
	/// Gets or sets the Redis connection string, which includes the host, port, and any necessary authentication details.
	/// </summary>
	public string ConnectionString { get; set; } = default!;

	/// <summary>
	/// Gets or sets the database ID to use for Redis operations. This allows multiple logical databases within a single Redis instance.
	/// </summary>
	public int DatabaseId { get; set; }

	/// <summary>
	/// Gets or sets the timeout for establishing a connection to the Redis server, in milliseconds.
	/// </summary>
	public int ConnectTimeout { get; set; }

	/// <summary>
	/// Gets or sets the timeout for synchronous operations on the Redis server, in milliseconds.
	/// </summary>
	public int SyncTimeout { get; set; }

	/// <summary>
	/// Gets or sets the retry policy for connection attempts to the Redis server
	/// This defines how many times the application should retry connecting to Redis in case of failure.
	/// </summary>
	public int ConnectRetry { get; set; }

	/// <summary>
	/// Gets or sets the retry policy for reconnection attempts to the Redis server.
	/// </summary>
	public int ReconnectRetryPolicy { get; set; }
}

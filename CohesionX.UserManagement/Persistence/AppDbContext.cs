using CohesionX.UserManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Persistence;

/// <summary>
/// Represents the Entity Framework Core database context for the User Management module,
/// managing entities such as users, jobs, statistics, and verification records.
/// </summary>
public class AppDbContext : DbContext
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AppDbContext"/> class using the specified options.
	/// </summary>
	/// <param name="options">The options to configure the context.</param>
	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
    {
    }

	/// <summary>
	/// Gets the <see cref="DbSet{TEntity}"/> for user entities.
	/// </summary>
	public DbSet<User> Users => Set<User>();

	/// <summary>
	/// Gets the <see cref="DbSet{TEntity}"/> for user dialect entities.
	/// </summary>
	public DbSet<UserDialect> UserDialects => Set<UserDialect>();

	/// <summary>
	/// Gets the <see cref="DbSet{TEntity}"/> for user statistics entities.
	/// </summary>
	public DbSet<UserStatistics> UserStatistics => Set<UserStatistics>();

	/// <summary>
	/// Gets the <see cref="DbSet{TEntity}"/> for Elo history entities.
	/// </summary>
	public DbSet<EloHistory> EloHistories => Set<EloHistory>();

	/// <summary>
	/// Gets the <see cref="DbSet{TEntity}"/> for job completion entities.
	/// </summary>
	public DbSet<JobCompletion> JobCompletions => Set<JobCompletion>();

	/// <summary>
	/// Gets the <see cref="DbSet{TEntity}"/> for job claim entities.
	/// </summary>
	public DbSet<JobClaim> JobClaims => Set<JobClaim>();

	/// <summary>
	/// Gets the <see cref="DbSet{TEntity}"/> for audit log entities.
	/// </summary>
	public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

	/// <summary>
	/// Gets the <see cref="DbSet{TEntity}"/> for verification record entities.
	/// </summary>
	public DbSet<VerificationRecord> VerificationRecords => Set<VerificationRecord>();

	/// <summary>
	/// Gets the <see cref="DbSet{TEntity}"/> for user verification requirement entities.
	/// </summary>
	public DbSet<UserVerificationRequirement> UserVerificationRequirements => Set<UserVerificationRequirement>();

	/// <summary>
	/// Configures the schema needed for the User Management entities,
	/// including keys, indexes, relationships, and delete behaviors.
	/// </summary>
	/// <param name="modelBuilder">The builder used to construct the model for the context.</param>
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Configure User entity
		modelBuilder.Entity<User>(entity =>
		{
			entity.HasKey(e => e.Id);

			// Enforce unique indexes on Email and IdNumber
			entity.HasIndex(e => e.Email).IsUnique();
			entity.HasIndex(e => e.IdNumber).IsUnique();

			// Configure one-to-many and one-to-one relationships with related entities
			entity.HasMany(u => u.Dialects)
				  .WithOne(d => d.User)
				  .HasForeignKey(d => d.UserId);

			entity.HasOne(u => u.Statistics)
				  .WithOne(s => s.User)
				  .HasForeignKey<UserStatistics>(s => s.UserId);

			entity.HasMany(u => u.EloHistories)
				  .WithOne(e => e.User)
				  .HasForeignKey(e => e.UserId);

			entity.HasMany(u => u.JobCompletions)
				  .WithOne(c => c.User)
				  .HasForeignKey(c => c.UserId);

			entity.HasMany(u => u.JobClaims)
				  .WithOne(j => j.User)
				  .HasForeignKey(j => j.UserId);

			entity.HasMany(u => u.AuditLogs)
				  .WithOne(a => a.User)
				  .HasForeignKey(a => a.UserId);

			entity.HasMany(u => u.VerificationRecords)
				  .WithOne(v => v.User)
				  .HasForeignKey(v => v.UserId);
		});

		// Configure EloHistory entity relationships with restricted delete behavior to prevent cascade deletes
		modelBuilder.Entity<EloHistory>()
			.HasOne(e => e.User)
			.WithMany(u => u.EloHistories)
			.HasForeignKey(e => e.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<EloHistory>()
			.HasOne(e => e.Comparison)
			.WithMany()
			.HasForeignKey(e => e.ComparisonId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}

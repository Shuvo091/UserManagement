using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Shared.Persistence;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	public DbSet<User> Users => Set<User>();
	public DbSet<UserDialect> UserDialects => Set<UserDialect>();
	public DbSet<UserStatistics> UserStatistics => Set<UserStatistics>();
	public DbSet<EloHistory> EloHistories => Set<EloHistory>();
	public DbSet<JobCompletion> JobCompletions => Set<JobCompletion>();
	public DbSet<JobClaim> JobClaims => Set<JobClaim>();
	public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
	public DbSet<VerificationRecord> VerificationRecords => Set<VerificationRecord>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<User>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.HasIndex(e => e.Email).IsUnique();
			entity.HasIndex(e => e.SouthAfricanIdNumber).IsUnique();

			entity.HasMany(u => u.Dialects).WithOne(d => d.User).HasForeignKey(d => d.UserId);
			entity.HasOne(u => u.Statistics).WithOne(s => s.User).HasForeignKey<UserStatistics>(s => s.UserId);
			entity.HasMany(u => u.EloHistories).WithOne(e => e.User).HasForeignKey(e => e.UserId);
			entity.HasMany(u => u.JobCompletions).WithOne(c => c.User).HasForeignKey(c => c.UserId);
			entity.HasMany(u => u.JobClaims).WithOne(j => j.User).HasForeignKey(j => j.UserId);
			entity.HasMany(u => u.AuditLogs).WithOne(a => a.User).HasForeignKey(a => a.UserId);
			entity.HasMany(u => u.VerificationRecords).WithOne(v => v.User).HasForeignKey(v => v.UserId);
		});
	}
}

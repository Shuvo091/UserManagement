using CohesionX.UserManagement.Database.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CohesionX.UserManagement.Database.EntityConfigurations;

/// <summary>
/// Configures the <see cref="User"/> entity's schema and relationships.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
	/// <summary>
	/// Configures the User entity properties, indexes, and relationships.
	/// </summary>
	/// <param name="builder">The builder used to configure the User entity.</param>
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.HasKey(e => e.Id);

		builder.HasIndex(e => e.Email).IsUnique();
		builder.HasIndex(e => e.IdNumber).IsUnique();

		builder.HasMany(u => u.Dialects)
			   .WithOne(d => d.User)
			   .HasForeignKey(d => d.UserId);

		builder.HasOne(u => u.Statistics)
			   .WithOne(s => s.User)
			   .HasForeignKey<UserStatistics>(s => s.UserId);

		builder.HasMany(u => u.EloHistories)
			   .WithOne(e => e.User)
			   .HasForeignKey(e => e.UserId);

		builder.HasMany(u => u.JobCompletions)
			   .WithOne(c => c.User)
			   .HasForeignKey(c => c.UserId);

		builder.HasMany(u => u.JobClaims)
			   .WithOne(j => j.User)
			   .HasForeignKey(j => j.UserId);

		builder.HasMany(u => u.AuditLogs)
			   .WithOne(a => a.User)
			   .HasForeignKey(a => a.UserId);

		builder.HasMany(u => u.VerificationRecords)
			   .WithOne(v => v.User)
			   .HasForeignKey(v => v.UserId);
	}
}

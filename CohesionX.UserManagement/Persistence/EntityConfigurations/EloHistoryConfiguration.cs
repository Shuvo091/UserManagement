using CohesionX.UserManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CohesionX.UserManagement.Persistence.EntityConfigurations;

/// <summary>
/// Configures the <see cref="EloHistory"/> entity and its relationships.
/// </summary>
public class EloHistoryConfiguration : IEntityTypeConfiguration<EloHistory>
{
	/// <summary>
	/// Applies configuration for the EloHistory entity including foreign keys and delete behavior.
	/// </summary>
	/// <param name="builder">The builder used to configure the EloHistory entity.</param>
	public void Configure(EntityTypeBuilder<EloHistory> builder)
	{
		builder.HasOne(e => e.User)
			   .WithMany(u => u.EloHistories)
			   .HasForeignKey(e => e.UserId)
			   .OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(e => e.Comparison)
			   .WithMany()
			   .HasForeignKey(e => e.ComparisonId)
			   .OnDelete(DeleteBehavior.Restrict);
	}
}

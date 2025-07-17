using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CohesionX.UserManagement.Modules.Users.Persistence.Config;

public class UserEntityTypeConfig : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.ToTable("users");

		builder.HasKey(u => u.Id);
		builder.HasIndex(u => u.Email).IsUnique();
		builder.Property(u => u.IdNumber).IsRequired();
		builder.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
	}
}

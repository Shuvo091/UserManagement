// <copyright file="VerificationRecordConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CohesionX.UserManagement.Database.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CohesionX.UserManagement.Database.EntityConfigurations;

/// <summary>
/// Configures the <see cref="VerificationRecord"/> entity's schema and relationships.
/// </summary>
public class VerificationRecordConfiguration : IEntityTypeConfiguration<VerificationRecord>
{
    /// <summary>
    /// Configures the VerificationRecord entity properties, indexes, and relationships.
    /// </summary>
    /// <param name="builder">The builder used to configure the VerificationRecord entity.</param>
    public void Configure(EntityTypeBuilder<VerificationRecord> builder)
    {
        builder.HasOne(v => v.Verifier)
        .WithMany()
        .HasForeignKey(v => v.VerifiedBy)
        .OnDelete(DeleteBehavior.Restrict);
    }
}

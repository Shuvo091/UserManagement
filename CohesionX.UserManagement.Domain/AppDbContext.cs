// <copyright file="AppDbContext.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CohesionX.UserManagement.Database.Abstractions.Contants;
using CohesionX.UserManagement.Database.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Database;

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
    public DbSet<User> Users => this.Set<User>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for user dialect entities.
    /// </summary>
    public DbSet<UserDialect> UserDialects => this.Set<UserDialect>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for user statistics entities.
    /// </summary>
    public DbSet<UserStatistics> UserStatistics => this.Set<UserStatistics>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for Elo history entities.
    /// </summary>
    public DbSet<EloHistory> EloHistories => this.Set<EloHistory>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for job completion entities.
    /// </summary>
    public DbSet<JobCompletion> JobCompletions => this.Set<JobCompletion>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for job claim entities.
    /// </summary>
    public DbSet<JobClaim> JobClaims => this.Set<JobClaim>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for audit log entities.
    /// </summary>
    public DbSet<AuditLog> AuditLogs => this.Set<AuditLog>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for verification record entities.
    /// </summary>
    public DbSet<VerificationRecord> VerificationRecords => this.Set<VerificationRecord>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for user verification requirement entities.
    /// </summary>
    public DbSet<UserVerificationRequirement> UserVerificationRequirements => this.Set<UserVerificationRequirement>();

    /// <summary>
    /// Configures the schema needed for the User Management entities,
    /// including keys, indexes, relationships, and delete behaviors.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for the context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DbSchema.Default);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

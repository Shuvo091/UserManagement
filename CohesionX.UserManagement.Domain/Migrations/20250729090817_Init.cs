// <copyright file="20250729090817_Init.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#nullable disable

namespace CohesionX.UserManagement.Migrations;

using System;
using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class Init : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "user_management");

        migrationBuilder.CreateTable(
            name: "Users",
            schema: "user_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FirstName = table.Column<string>(type: "text", nullable: false),
                LastName = table.Column<string>(type: "text", nullable: false),
                Email = table.Column<string>(type: "text", nullable: false),
                UserName = table.Column<string>(type: "text", nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: false),
                Phone = table.Column<string>(type: "text", nullable: true),
                IdNumber = table.Column<string>(type: "text", nullable: true),
                Status = table.Column<string>(type: "text", nullable: false),
                Role = table.Column<string>(type: "text", nullable: false),
                IsProfessional = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UserVerificationRequirements",
            schema: "user_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RequireIdDocument = table.Column<bool>(type: "boolean", nullable: false),
                RequirePhotoUpload = table.Column<bool>(type: "boolean", nullable: false),
                RequirePhoneVerification = table.Column<bool>(type: "boolean", nullable: false),
                RequireEmailVerification = table.Column<bool>(type: "boolean", nullable: false),
                VerificationLevel = table.Column<string>(type: "text", nullable: false),
                ValidationRulesJson = table.Column<string>(type: "text", nullable: false),
                Reason = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserVerificationRequirements", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            schema: "user_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<string>(type: "text", nullable: false),
                DetailsJson = table.Column<string>(type: "text", nullable: false),
                IpAddress = table.Column<string>(type: "text", nullable: false),
                UserAgent = table.Column<string>(type: "text", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_AuditLogs_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "user_management",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "EloHistories",
            schema: "user_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                OldElo = table.Column<int>(type: "integer", nullable: false),
                NewElo = table.Column<int>(type: "integer", nullable: false),
                OpponentElo = table.Column<int>(type: "integer", nullable: false),
                Reason = table.Column<string>(type: "text", nullable: false),
                ComparisonId = table.Column<Guid>(type: "uuid", nullable: false),
                JobId = table.Column<string>(type: "text", nullable: false),
                Outcome = table.Column<string>(type: "text", nullable: false),
                ComparisonType = table.Column<string>(type: "text", nullable: false),
                KFactorUsed = table.Column<int>(type: "integer", nullable: false),
                ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EloHistories", x => x.Id);
                table.ForeignKey(
                    name: "FK_EloHistories_Users_ComparisonId",
                    column: x => x.ComparisonId,
                    principalSchema: "user_management",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_EloHistories_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "user_management",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "JobClaims",
            schema: "user_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                JobId = table.Column<string>(type: "text", nullable: false),
                ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                BookOutExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JobClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_JobClaims_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "user_management",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "JobCompletions",
            schema: "user_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                JobId = table.Column<string>(type: "text", nullable: false),
                Outcome = table.Column<string>(type: "text", nullable: false),
                EloChange = table.Column<int>(type: "integer", nullable: false),
                ComparisonId = table.Column<Guid>(type: "uuid", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JobCompletions", x => x.Id);
                table.ForeignKey(
                    name: "FK_JobCompletions_Users_ComparisonId",
                    column: x => x.ComparisonId,
                    principalSchema: "user_management",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_JobCompletions_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "user_management",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserDialects",
            schema: "user_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Dialect = table.Column<string>(type: "text", nullable: false),
                ProficiencyLevel = table.Column<string>(type: "text", nullable: false),
                IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserDialects", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserDialects_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "user_management",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserStatistics",
            schema: "user_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                TotalJobs = table.Column<int>(type: "integer", nullable: false),
                CurrentElo = table.Column<int>(type: "integer", nullable: false),
                PeakElo = table.Column<int>(type: "integer", nullable: false),
                GamesPlayed = table.Column<int>(type: "integer", nullable: false),
                LastCalculated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserStatistics", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserStatistics_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "user_management",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VerificationRecords",
            schema: "user_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                VerificationType = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                VerificationLevel = table.Column<string>(type: "text", nullable: false),
                VerificationData = table.Column<string>(type: "text", nullable: false),
                VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VerificationRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_VerificationRecords_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "user_management",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_UserId",
            schema: "user_management",
            table: "AuditLogs",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_EloHistories_ComparisonId",
            schema: "user_management",
            table: "EloHistories",
            column: "ComparisonId");

        migrationBuilder.CreateIndex(
            name: "IX_EloHistories_UserId",
            schema: "user_management",
            table: "EloHistories",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_JobClaims_UserId",
            schema: "user_management",
            table: "JobClaims",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_JobCompletions_ComparisonId",
            schema: "user_management",
            table: "JobCompletions",
            column: "ComparisonId");

        migrationBuilder.CreateIndex(
            name: "IX_JobCompletions_UserId",
            schema: "user_management",
            table: "JobCompletions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserDialects_UserId",
            schema: "user_management",
            table: "UserDialects",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            schema: "user_management",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_IdNumber",
            schema: "user_management",
            table: "Users",
            column: "IdNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserStatistics_UserId",
            schema: "user_management",
            table: "UserStatistics",
            column: "UserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_VerificationRecords_UserId",
            schema: "user_management",
            table: "VerificationRecords",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AuditLogs",
            schema: "user_management");

        migrationBuilder.DropTable(
            name: "EloHistories",
            schema: "user_management");

        migrationBuilder.DropTable(
            name: "JobClaims",
            schema: "user_management");

        migrationBuilder.DropTable(
            name: "JobCompletions",
            schema: "user_management");

        migrationBuilder.DropTable(
            name: "UserDialects",
            schema: "user_management");

        migrationBuilder.DropTable(
            name: "UserStatistics",
            schema: "user_management");

        migrationBuilder.DropTable(
            name: "UserVerificationRequirements",
            schema: "user_management");

        migrationBuilder.DropTable(
            name: "VerificationRecords",
            schema: "user_management");

        migrationBuilder.DropTable(
            name: "Users",
            schema: "user_management");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CohesionX.UserManagement.Migrations
{
    /// <inheritdoc />
    public partial class initialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    IdNumber = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsProfessional = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    DetailsJson = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EloHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldElo = table.Column<int>(type: "integer", nullable: false),
                    NewElo = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    ComparisonId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<string>(type: "text", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    ComparisonType = table.Column<string>(type: "text", nullable: false),
                    KFactorUsed = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EloHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EloHistories_Users_ComparisonId",
                        column: x => x.ComparisonId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EloHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<string>(type: "text", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BookOutExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<string>(type: "text", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    EloChange = table.Column<int>(type: "integer", nullable: false),
                    ComparisonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobCompletions_Users_ComparisonId",
                        column: x => x.ComparisonId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobCompletions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDialects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dialect = table.Column<string>(type: "text", nullable: false),
                    ProficiencyLevel = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDialects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDialects_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserStatistics",
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
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStatistics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerificationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    VerificationLevel = table.Column<string>(type: "text", nullable: false),
                    VerificationData = table.Column<string>(type: "text", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EloHistories_ComparisonId",
                table: "EloHistories",
                column: "ComparisonId");

            migrationBuilder.CreateIndex(
                name: "IX_EloHistories_UserId",
                table: "EloHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobClaims_UserId",
                table: "JobClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobCompletions_ComparisonId",
                table: "JobCompletions",
                column: "ComparisonId");

            migrationBuilder.CreateIndex(
                name: "IX_JobCompletions_UserId",
                table: "JobCompletions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDialects_UserId",
                table: "UserDialects",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IdNumber",
                table: "Users",
                column: "IdNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStatistics_UserId",
                table: "UserStatistics",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRecords_UserId",
                table: "VerificationRecords",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "EloHistories");

            migrationBuilder.DropTable(
                name: "JobClaims");

            migrationBuilder.DropTable(
                name: "JobCompletions");

            migrationBuilder.DropTable(
                name: "UserDialects");

            migrationBuilder.DropTable(
                name: "UserStatistics");

            migrationBuilder.DropTable(
                name: "VerificationRecords");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CohesionX.UserManagement.Migrations;

/// <inheritdoc />
public partial class FixIdNumberConstraint : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Users_IdNumber",
            schema: "user_management",
            table: "Users");

        migrationBuilder.CreateIndex(
            name: "IX_Users_IdNumber",
            schema: "user_management",
            table: "Users",
            column: "IdNumber",
            unique: true,
            filter: "\"IdNumber\" IS NOT NULL AND \"IdNumber\" <> ''");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Users_IdNumber",
            schema: "user_management",
            table: "Users");

        migrationBuilder.CreateIndex(
            name: "IX_Users_IdNumber",
            schema: "user_management",
            table: "Users",
            column: "IdNumber",
            unique: true);
    }
}

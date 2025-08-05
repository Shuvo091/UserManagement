using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CohesionX.UserManagement.Migrations;

/// <inheritdoc />
public partial class AddUserIdFKForVerificationRequirements : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "UserId",
            schema: "user_management",
            table: "UserVerificationRequirements",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateIndex(
            name: "IX_UserVerificationRequirements_UserId",
            schema: "user_management",
            table: "UserVerificationRequirements",
            column: "UserId",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_UserVerificationRequirements_Users_UserId",
            schema: "user_management",
            table: "UserVerificationRequirements",
            column: "UserId",
            principalSchema: "user_management",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_UserVerificationRequirements_Users_UserId",
            schema: "user_management",
            table: "UserVerificationRequirements");

        migrationBuilder.DropIndex(
            name: "IX_UserVerificationRequirements_UserId",
            schema: "user_management",
            table: "UserVerificationRequirements");

        migrationBuilder.DropColumn(
            name: "UserId",
            schema: "user_management",
            table: "UserVerificationRequirements");
    }
}

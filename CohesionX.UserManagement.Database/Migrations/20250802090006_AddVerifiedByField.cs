using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CohesionX.UserManagement.Migrations;

/// <inheritdoc />
public partial class AddVerifiedByField : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "VerifiedBy",
            schema: "user_management",
            table: "VerificationRecords",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_VerificationRecords_VerifiedBy",
            schema: "user_management",
            table: "VerificationRecords",
            column: "VerifiedBy");

        migrationBuilder.AddForeignKey(
            name: "FK_VerificationRecords_Users_VerifiedBy",
            schema: "user_management",
            table: "VerificationRecords",
            column: "VerifiedBy",
            principalSchema: "user_management",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_VerificationRecords_Users_VerifiedBy",
            schema: "user_management",
            table: "VerificationRecords");

        migrationBuilder.DropIndex(
            name: "IX_VerificationRecords_VerifiedBy",
            schema: "user_management",
            table: "VerificationRecords");

        migrationBuilder.DropColumn(
            name: "VerifiedBy",
            schema: "user_management",
            table: "VerificationRecords");
    }
}

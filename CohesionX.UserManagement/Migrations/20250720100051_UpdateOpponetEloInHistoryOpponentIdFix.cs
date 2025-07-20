using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CohesionX.UserManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOpponetEloInHistoryOpponentIdFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EloHistories_Users_UserId1",
                table: "EloHistories");

            migrationBuilder.DropIndex(
                name: "IX_EloHistories_UserId1",
                table: "EloHistories");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "EloHistories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "EloHistories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EloHistories_UserId1",
                table: "EloHistories",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_EloHistories_Users_UserId1",
                table: "EloHistories",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}

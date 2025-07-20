using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CohesionX.UserManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOpponetEloInHistoryOpponentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EloHistories_Users_ComparisonId",
                table: "EloHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_EloHistories_Users_UserId",
                table: "EloHistories");

            migrationBuilder.AddColumn<Guid>(
                name: "OpponentId",
                table: "EloHistories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "EloHistories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EloHistories_OpponentId",
                table: "EloHistories",
                column: "OpponentId");

            migrationBuilder.CreateIndex(
                name: "IX_EloHistories_UserId1",
                table: "EloHistories",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_EloHistories_Users_ComparisonId",
                table: "EloHistories",
                column: "ComparisonId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EloHistories_Users_OpponentId",
                table: "EloHistories",
                column: "OpponentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EloHistories_Users_UserId",
                table: "EloHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EloHistories_Users_UserId1",
                table: "EloHistories",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EloHistories_Users_ComparisonId",
                table: "EloHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_EloHistories_Users_OpponentId",
                table: "EloHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_EloHistories_Users_UserId",
                table: "EloHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_EloHistories_Users_UserId1",
                table: "EloHistories");

            migrationBuilder.DropIndex(
                name: "IX_EloHistories_OpponentId",
                table: "EloHistories");

            migrationBuilder.DropIndex(
                name: "IX_EloHistories_UserId1",
                table: "EloHistories");

            migrationBuilder.DropColumn(
                name: "OpponentId",
                table: "EloHistories");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "EloHistories");

            migrationBuilder.AddForeignKey(
                name: "FK_EloHistories_Users_ComparisonId",
                table: "EloHistories",
                column: "ComparisonId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EloHistories_Users_UserId",
                table: "EloHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

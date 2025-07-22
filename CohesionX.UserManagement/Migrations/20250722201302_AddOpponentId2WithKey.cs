using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CohesionX.UserManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddOpponentId2WithKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EloHistories_OpponentId2",
                table: "EloHistories",
                column: "OpponentId2");

            migrationBuilder.AddForeignKey(
                name: "FK_EloHistories_Users_OpponentId2",
                table: "EloHistories",
                column: "OpponentId2",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EloHistories_Users_OpponentId2",
                table: "EloHistories");

            migrationBuilder.DropIndex(
                name: "IX_EloHistories_OpponentId2",
                table: "EloHistories");
        }
    }
}

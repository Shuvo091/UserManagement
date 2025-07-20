using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CohesionX.UserManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOpponetEloInHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OpponentElo",
                table: "EloHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpponentElo",
                table: "EloHistories");
        }
    }
}

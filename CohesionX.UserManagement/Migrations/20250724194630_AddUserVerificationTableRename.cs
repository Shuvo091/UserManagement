using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CohesionX.UserManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddUserVerificationTableRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserVerificationRequiremens",
                table: "UserVerificationRequiremens");

            migrationBuilder.RenameTable(
                name: "UserVerificationRequiremens",
                newName: "UserVerificationRequirements");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserVerificationRequirements",
                table: "UserVerificationRequirements",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserVerificationRequirements",
                table: "UserVerificationRequirements");

            migrationBuilder.RenameTable(
                name: "UserVerificationRequirements",
                newName: "UserVerificationRequiremens");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserVerificationRequiremens",
                table: "UserVerificationRequiremens",
                column: "Id");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class FixUserDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployerDetails_UsersId",
                table: "EmployerDetails");

            migrationBuilder.CreateIndex(
                name: "IX_EmployerDetails_UsersId",
                table: "EmployerDetails",
                column: "UsersId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployerDetails_UsersId",
                table: "EmployerDetails");

            migrationBuilder.CreateIndex(
                name: "IX_EmployerDetails_UsersId",
                table: "EmployerDetails",
                column: "UsersId");
        }
    }
}

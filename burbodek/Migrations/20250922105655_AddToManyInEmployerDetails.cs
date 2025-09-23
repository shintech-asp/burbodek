using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class AddToManyInEmployerDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployerDetailsId",
                table: "Files",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Files_EmployerDetailsId",
                table: "Files",
                column: "EmployerDetailsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_EmployerDetails_EmployerDetailsId",
                table: "Files",
                column: "EmployerDetailsId",
                principalTable: "EmployerDetails",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_EmployerDetails_EmployerDetailsId",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_EmployerDetailsId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "EmployerDetailsId",
                table: "Files");
        }
    }
}

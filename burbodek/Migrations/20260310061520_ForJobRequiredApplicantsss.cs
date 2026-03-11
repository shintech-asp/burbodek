using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class ForJobRequiredApplicantsss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WillHire",
                table: "Jobs",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ExpectedSalary",
                table: "JobApplication",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WillHire",
                table: "Jobs");

            migrationBuilder.AlterColumn<int>(
                name: "ExpectedSalary",
                table: "JobApplication",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}

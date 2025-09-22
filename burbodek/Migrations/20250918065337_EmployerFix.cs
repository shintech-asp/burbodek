using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class EmployerFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "EmployerDetails");

            migrationBuilder.DropColumn(
                name: "EmployerName",
                table: "EmployerDetails");

            migrationBuilder.AlterColumn<int>(
                name: "isTrainingCenter",
                table: "EmployerDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "isEmployer",
                table: "EmployerDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "isTrainingCenter",
                table: "EmployerDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "isEmployer",
                table: "EmployerDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "EmployerDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployerName",
                table: "EmployerDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}

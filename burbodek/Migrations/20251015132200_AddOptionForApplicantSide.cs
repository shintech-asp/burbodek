using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class AddOptionForApplicantSide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CV",
                table: "JobApplication");

            migrationBuilder.AddColumn<string>(
                name: "Coe",
                table: "JobApplication",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Diploma",
                table: "JobApplication",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PassportId",
                table: "JobApplication",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Resume",
                table: "JobApplication",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeamansBook",
                table: "JobApplication",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tor",
                table: "JobApplication",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Coe",
                table: "JobApplication");

            migrationBuilder.DropColumn(
                name: "Diploma",
                table: "JobApplication");

            migrationBuilder.DropColumn(
                name: "PassportId",
                table: "JobApplication");

            migrationBuilder.DropColumn(
                name: "Resume",
                table: "JobApplication");

            migrationBuilder.DropColumn(
                name: "SeamansBook",
                table: "JobApplication");

            migrationBuilder.DropColumn(
                name: "Tor",
                table: "JobApplication");

            migrationBuilder.AddColumn<string>(
                name: "CV",
                table: "JobApplication",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}

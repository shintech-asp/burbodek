using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class ApplyLatestChangesasoftoday : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBirCertificate",
                table: "EmployerDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBusinessDescription",
                table: "EmployerDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBusinessName",
                table: "EmployerDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBusinessPermit",
                table: "EmployerDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPoeaLicense",
                table: "EmployerDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProofPartnerShip",
                table: "EmployerDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSecDti",
                table: "EmployerDetails",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBirCertificate",
                table: "EmployerDetails");

            migrationBuilder.DropColumn(
                name: "IsBusinessDescription",
                table: "EmployerDetails");

            migrationBuilder.DropColumn(
                name: "IsBusinessName",
                table: "EmployerDetails");

            migrationBuilder.DropColumn(
                name: "IsBusinessPermit",
                table: "EmployerDetails");

            migrationBuilder.DropColumn(
                name: "IsPoeaLicense",
                table: "EmployerDetails");

            migrationBuilder.DropColumn(
                name: "IsProofPartnerShip",
                table: "EmployerDetails");

            migrationBuilder.DropColumn(
                name: "IsSecDti",
                table: "EmployerDetails");
        }
    }
}

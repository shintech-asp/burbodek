using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class AuthOTPAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Otpcode",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Otpexpiration",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Otpsent",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isVerified",
                table: "Users",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegistrationCount",
                table: "EmployerDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isAllowedForResubmission",
                table: "EmployerDetails",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Otpcode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Otpexpiration",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Otpsent",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "isVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RegistrationCount",
                table: "EmployerDetails");

            migrationBuilder.DropColumn(
                name: "isAllowedForResubmission",
                table: "EmployerDetails");
        }
    }
}

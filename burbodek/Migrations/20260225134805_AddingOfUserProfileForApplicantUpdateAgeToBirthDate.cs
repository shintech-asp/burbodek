using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class AddingOfUserProfileForApplicantUpdateAgeToBirthDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "UserProfile");

            migrationBuilder.AddColumn<DateOnly>(
                name: "Birthdate",
                table: "UserProfile",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Birthdate",
                table: "UserProfile");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "UserProfile",
                type: "int",
                nullable: true);
        }
    }
}

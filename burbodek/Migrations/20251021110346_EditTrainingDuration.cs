using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class EditTrainingDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationFrom",
                table: "Training");

            migrationBuilder.DropColumn(
                name: "DurationTo",
                table: "Training");

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "Training",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Training");

            migrationBuilder.AddColumn<DateTime>(
                name: "DurationFrom",
                table: "Training",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DurationTo",
                table: "Training",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}

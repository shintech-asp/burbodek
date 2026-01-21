using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class TrainingBadgeValidityFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Validity",
                table: "TrainingBadge");

            migrationBuilder.AddColumn<int>(
                name: "Validity",
                table: "TrainingBadge",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Validity",
                table: "TrainingBadge");

            migrationBuilder.AddColumn<DateTime>(
                name: "Validity",
                table: "TrainingBadge",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");
        }

    }
}

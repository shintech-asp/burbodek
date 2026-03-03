using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalStageOfReportingForFinalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isFinal",
                table: "Training",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isFinal",
                table: "Jobs",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isFinal",
                table: "Training");

            migrationBuilder.DropColumn(
                name: "isFinal",
                table: "Jobs");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class TrainingBadgeFixList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingBadge_TrainingId",
                table: "TrainingBadge");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingBadge_TrainingId",
                table: "TrainingBadge",
                column: "TrainingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingBadge_TrainingId",
                table: "TrainingBadge");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingBadge_TrainingId",
                table: "TrainingBadge",
                column: "TrainingId");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMigrationForInterview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Interview_JobApplication_JobApplicationId",
                table: "Interview");

            migrationBuilder.RenameColumn(
                name: "JobApplicationId",
                table: "Interview",
                newName: "JobsId");

            migrationBuilder.RenameIndex(
                name: "IX_Interview_JobApplicationId",
                table: "Interview",
                newName: "IX_Interview_JobsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Interview_Jobs_JobsId",
                table: "Interview",
                column: "JobsId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Interview_Jobs_JobsId",
                table: "Interview");

            migrationBuilder.RenameColumn(
                name: "JobsId",
                table: "Interview",
                newName: "JobApplicationId");

            migrationBuilder.RenameIndex(
                name: "IX_Interview_JobsId",
                table: "Interview",
                newName: "IX_Interview_JobApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Interview_JobApplication_JobApplicationId",
                table: "Interview",
                column: "JobApplicationId",
                principalTable: "JobApplication",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

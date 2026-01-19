using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class AddJobApplicantUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicantJobUpload",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobUploadsId = table.Column<int>(type: "int", nullable: false),
                    Upload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobApplicationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantJobUpload", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicantJobUpload_JobApplication_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplication",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicantJobUpload_JobUploads_JobUploadsId",
                        column: x => x.JobUploadsId,
                        principalTable: "JobUploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantJobUpload_JobApplicationId",
                table: "ApplicantJobUpload",
                column: "JobApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantJobUpload_JobUploadsId",
                table: "ApplicantJobUpload",
                column: "JobUploadsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicantJobUpload");
        }
    }
}

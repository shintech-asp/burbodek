using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingApplicationUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicantTrainingUpload",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingUploadsId = table.Column<int>(type: "int", nullable: false),
                    Upload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrainingApplicationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantTrainingUpload", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicantTrainingUpload_TrainingApplication_TrainingApplicationId",
                        column: x => x.TrainingApplicationId,
                        principalTable: "TrainingApplication",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicantTrainingUpload_TrainingUploads_TrainingUploadsId",
                        column: x => x.TrainingUploadsId,
                        principalTable: "TrainingUploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantTrainingUpload_TrainingApplicationId",
                table: "ApplicantTrainingUpload",
                column: "TrainingApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantTrainingUpload_TrainingUploadsId",
                table: "ApplicantTrainingUpload",
                column: "TrainingUploadsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicantTrainingUpload");
        }
    }
}

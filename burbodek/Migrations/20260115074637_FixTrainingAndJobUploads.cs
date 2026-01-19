using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class FixTrainingAndJobUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsersId",
                table: "ApplicantTrainingUpload",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsersId",
                table: "ApplicantJobUpload",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantTrainingUpload_UsersId",
                table: "ApplicantTrainingUpload",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantJobUpload_UsersId",
                table: "ApplicantJobUpload",
                column: "UsersId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicantJobUpload_Users_UsersId",
                table: "ApplicantJobUpload",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicantTrainingUpload_Users_UsersId",
                table: "ApplicantTrainingUpload",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicantJobUpload_Users_UsersId",
                table: "ApplicantJobUpload");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicantTrainingUpload_Users_UsersId",
                table: "ApplicantTrainingUpload");

            migrationBuilder.DropIndex(
                name: "IX_ApplicantTrainingUpload_UsersId",
                table: "ApplicantTrainingUpload");

            migrationBuilder.DropIndex(
                name: "IX_ApplicantJobUpload_UsersId",
                table: "ApplicantJobUpload");

            migrationBuilder.DropColumn(
                name: "UsersId",
                table: "ApplicantTrainingUpload");

            migrationBuilder.DropColumn(
                name: "UsersId",
                table: "ApplicantJobUpload");
        }
    }
}

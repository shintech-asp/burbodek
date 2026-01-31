using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class addFkinTrainignApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsersId",
                table: "TrainingApplication",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingApplication_UsersId",
                table: "TrainingApplication",
                column: "UsersId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingApplication_Users_UsersId",
                table: "TrainingApplication",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingApplication_Users_UsersId",
                table: "TrainingApplication");

            migrationBuilder.DropIndex(
                name: "IX_TrainingApplication_UsersId",
                table: "TrainingApplication");

            migrationBuilder.DropColumn(
                name: "UsersId",
                table: "TrainingApplication");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class addFkinTrainignApplicationAndJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobApplication_Users_UsersId",
                table: "JobApplication");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingApplication_Users_UsersId",
                table: "TrainingApplication");

            migrationBuilder.DropIndex(
                name: "IX_TrainingApplication_UsersId",
                table: "TrainingApplication");

            migrationBuilder.DropIndex(
                name: "IX_JobApplication_UsersId",
                table: "JobApplication");

            migrationBuilder.DropColumn(
                name: "UsersId",
                table: "TrainingApplication");

            migrationBuilder.DropColumn(
                name: "UsersId",
                table: "JobApplication");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingApplication_AppliedBy",
                table: "TrainingApplication",
                column: "AppliedBy");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplication_AppliedBy",
                table: "JobApplication",
                column: "AppliedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_JobApplication_Users_AppliedBy",
                table: "JobApplication",
                column: "AppliedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingApplication_Users_AppliedBy",
                table: "TrainingApplication",
                column: "AppliedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobApplication_Users_AppliedBy",
                table: "JobApplication");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingApplication_Users_AppliedBy",
                table: "TrainingApplication");

            migrationBuilder.DropIndex(
                name: "IX_TrainingApplication_AppliedBy",
                table: "TrainingApplication");

            migrationBuilder.DropIndex(
                name: "IX_JobApplication_AppliedBy",
                table: "JobApplication");

            migrationBuilder.AddColumn<int>(
                name: "UsersId",
                table: "TrainingApplication",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsersId",
                table: "JobApplication",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingApplication_UsersId",
                table: "TrainingApplication",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplication_UsersId",
                table: "JobApplication",
                column: "UsersId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobApplication_Users_UsersId",
                table: "JobApplication",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingApplication_Users_UsersId",
                table: "TrainingApplication",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}

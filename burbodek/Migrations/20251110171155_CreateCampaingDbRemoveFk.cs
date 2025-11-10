using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class CreateCampaingDbRemoveFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaign_Jobs_SelectedJobId",
                table: "Campaign");

            migrationBuilder.DropForeignKey(
                name: "FK_Campaign_Training_SelectedTrainingId",
                table: "Campaign");

            migrationBuilder.AlterColumn<int>(
                name: "SelectedTrainingId",
                table: "Campaign",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SelectedJobId",
                table: "Campaign",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaign_Jobs_SelectedJobId",
                table: "Campaign",
                column: "SelectedJobId",
                principalTable: "Jobs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaign_Training_SelectedTrainingId",
                table: "Campaign",
                column: "SelectedTrainingId",
                principalTable: "Training",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaign_Jobs_SelectedJobId",
                table: "Campaign");

            migrationBuilder.DropForeignKey(
                name: "FK_Campaign_Training_SelectedTrainingId",
                table: "Campaign");

            migrationBuilder.AlterColumn<int>(
                name: "SelectedTrainingId",
                table: "Campaign",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SelectedJobId",
                table: "Campaign",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Campaign_Jobs_SelectedJobId",
                table: "Campaign",
                column: "SelectedJobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Campaign_Training_SelectedTrainingId",
                table: "Campaign",
                column: "SelectedTrainingId",
                principalTable: "Training",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

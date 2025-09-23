using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class FixUsersToEmployerDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_EmployerDetails_EmployerDetailsId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentDetails_EmployerDetails_EmployerDetailsId",
                table: "PaymentDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscription_EmployerDetails_EmployerDetailsId",
                table: "Subscription");

            migrationBuilder.DropIndex(
                name: "IX_Subscription_EmployerDetailsId",
                table: "Subscription");

            migrationBuilder.DropIndex(
                name: "IX_PaymentDetails_EmployerDetailsId",
                table: "PaymentDetails");

            migrationBuilder.DropIndex(
                name: "IX_Files_EmployerDetailsId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "EmployerDetailsId",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "EmployerDetailsId",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "EmployerDetailsId",
                table: "Files");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployerDetailsId",
                table: "Subscription",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployerDetailsId",
                table: "PaymentDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployerDetailsId",
                table: "Files",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_EmployerDetailsId",
                table: "Subscription",
                column: "EmployerDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDetails_EmployerDetailsId",
                table: "PaymentDetails",
                column: "EmployerDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_EmployerDetailsId",
                table: "Files",
                column: "EmployerDetailsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_EmployerDetails_EmployerDetailsId",
                table: "Files",
                column: "EmployerDetailsId",
                principalTable: "EmployerDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentDetails_EmployerDetails_EmployerDetailsId",
                table: "PaymentDetails",
                column: "EmployerDetailsId",
                principalTable: "EmployerDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscription_EmployerDetails_EmployerDetailsId",
                table: "Subscription",
                column: "EmployerDetailsId",
                principalTable: "EmployerDetails",
                principalColumn: "Id");
        }
    }
}

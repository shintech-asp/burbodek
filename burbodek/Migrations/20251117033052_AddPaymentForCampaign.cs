using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentForCampaign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentDetailsId",
                table: "Campaign",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campaign_PaymentDetailsId",
                table: "Campaign",
                column: "PaymentDetailsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaign_PaymentDetails_PaymentDetailsId",
                table: "Campaign",
                column: "PaymentDetailsId",
                principalTable: "PaymentDetails",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaign_PaymentDetails_PaymentDetailsId",
                table: "Campaign");

            migrationBuilder.DropIndex(
                name: "IX_Campaign_PaymentDetailsId",
                table: "Campaign");

            migrationBuilder.DropColumn(
                name: "PaymentDetailsId",
                table: "Campaign");
        }
    }
}

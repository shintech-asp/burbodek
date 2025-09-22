using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class FixSomething : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployerDetailsId",
                table: "Subscription",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_EmployerDetailsId",
                table: "Subscription",
                column: "EmployerDetailsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscription_EmployerDetails_EmployerDetailsId",
                table: "Subscription",
                column: "EmployerDetailsId",
                principalTable: "EmployerDetails",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscription_EmployerDetails_EmployerDetailsId",
                table: "Subscription");

            migrationBuilder.DropIndex(
                name: "IX_Subscription_EmployerDetailsId",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "EmployerDetailsId",
                table: "Subscription");
        }
    }
}

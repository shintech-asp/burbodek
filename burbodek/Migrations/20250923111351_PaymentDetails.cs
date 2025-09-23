using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class PaymentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsersId = table.Column<int>(type: "int", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployerDetailsId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentDetails_EmployerDetails_EmployerDetailsId",
                        column: x => x.EmployerDetailsId,
                        principalTable: "EmployerDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentDetails_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDetails_EmployerDetailsId",
                table: "PaymentDetails",
                column: "EmployerDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDetails_UsersId",
                table: "PaymentDetails",
                column: "UsersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentDetails");
        }
    }
}

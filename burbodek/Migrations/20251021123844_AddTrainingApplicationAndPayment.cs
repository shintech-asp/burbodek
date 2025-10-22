using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingApplicationAndPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingApplication",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingId = table.Column<int>(type: "int", nullable: false),
                    AppliedBy = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MobileNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Diploma = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resume = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PassportId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SeamansBook = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Coe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingApplication", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingApplication_Training_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Training",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsersId = table.Column<int>(type: "int", nullable: false),
                    TrainingApplicationId = table.Column<int>(type: "int", nullable: false),
                    PaymentOption = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Paid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ModeOfPayment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingPayments_TrainingApplication_TrainingApplicationId",
                        column: x => x.TrainingApplicationId,
                        principalTable: "TrainingApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingPayments_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingApplication_TrainingId",
                table: "TrainingApplication",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPayments_TrainingApplicationId",
                table: "TrainingPayments",
                column: "TrainingApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPayments_UsersId",
                table: "TrainingPayments",
                column: "UsersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingPayments");

            migrationBuilder.DropTable(
                name: "TrainingApplication");
        }
    }
}

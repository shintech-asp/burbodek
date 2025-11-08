using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace burbodek.Migrations
{
    /// <inheritdoc />
    public partial class AddFkTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "EmailTemplate",
                newName: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplate_UsersId",
                table: "EmailTemplate",
                column: "UsersId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailTemplate_Users_UsersId",
                table: "EmailTemplate",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailTemplate_Users_UsersId",
                table: "EmailTemplate");

            migrationBuilder.DropIndex(
                name: "IX_EmailTemplate_UsersId",
                table: "EmailTemplate");

            migrationBuilder.RenameColumn(
                name: "UsersId",
                table: "EmailTemplate",
                newName: "CreatedBy");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    /// <inheritdoc />
    public partial class ArreglarRelacionUsuarioApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_AspNetUsers_ApplicationUserId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ApplicationUserId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Usuarios");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdApplicationUser",
                table: "Usuarios",
                column: "IdApplicationUser");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_AspNetUsers_IdApplicationUser",
                table: "Usuarios",
                column: "IdApplicationUser",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_AspNetUsers_IdApplicationUser",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_IdApplicationUser",
                table: "Usuarios");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Usuarios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ApplicationUserId",
                table: "Usuarios",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_AspNetUsers_ApplicationUserId",
                table: "Usuarios",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

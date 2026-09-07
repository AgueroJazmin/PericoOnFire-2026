
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    /// <inheritdoc />
    public partial class Arreglar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosCaja_Usuarios_UsuarioId",
                table: "MovimientosCaja");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Comandas_ComandaId",
                table: "Pagos");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Usuarios_UsuarioCajaId",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_ComandaId",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_UsuarioCajaId",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_UsuarioId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "ComandaId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "UsuarioCajaId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "MovimientosCaja");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdComanda",
                table: "Pagos",
                column: "IdComanda");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdUsuarioCaja",
                table: "Pagos",
                column: "IdUsuarioCaja");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_IdUsuario",
                table: "MovimientosCaja",
                column: "IdUsuario");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosCaja_Usuarios_IdUsuario",
                table: "MovimientosCaja",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Comandas_IdComanda",
                table: "Pagos",
                column: "IdComanda",
                principalTable: "Comandas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Usuarios_IdUsuarioCaja",
                table: "Pagos",
                column: "IdUsuarioCaja",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosCaja_Usuarios_IdUsuario",
                table: "MovimientosCaja");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Comandas_IdComanda",
                table: "Pagos");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Usuarios_IdUsuarioCaja",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_IdComanda",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_IdUsuarioCaja",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_IdUsuario",
                table: "MovimientosCaja");

            migrationBuilder.AddColumn<int>(
                name: "ComandaId",
                table: "Pagos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioCajaId",
                table: "Pagos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "MovimientosCaja",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_ComandaId",
                table: "Pagos",
                column: "ComandaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_UsuarioCajaId",
                table: "Pagos",
                column: "UsuarioCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_UsuarioId",
                table: "MovimientosCaja",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosCaja_Usuarios_UsuarioId",
                table: "MovimientosCaja",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Comandas_ComandaId",
                table: "Pagos",
                column: "ComandaId",
                principalTable: "Comandas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Usuarios_UsuarioCajaId",
                table: "Pagos",
                column: "UsuarioCajaId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

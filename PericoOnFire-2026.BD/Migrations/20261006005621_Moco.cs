using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    /// <inheritdoc />
    public partial class Moco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosCaja_TurnosCaja_TurnoCajaId",
                table: "MovimientosCaja");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_TurnosCaja_TurnoCajaId",
                table: "Pagos");

            migrationBuilder.DropForeignKey(
                name: "FK_TurnosCaja_Usuarios_UsuarioAperturaId",
                table: "TurnosCaja");

            migrationBuilder.DropForeignKey(
                name: "FK_TurnosCaja_Usuarios_UsuarioCierreId",
                table: "TurnosCaja");

            migrationBuilder.DropIndex(
                name: "IX_TurnosCaja_UsuarioAperturaId",
                table: "TurnosCaja");

            migrationBuilder.DropIndex(
                name: "IX_TurnosCaja_UsuarioCierreId",
                table: "TurnosCaja");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_TurnoCajaId",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_TurnoCajaId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "UsuarioAperturaId",
                table: "TurnosCaja");

            migrationBuilder.DropColumn(
                name: "UsuarioCierreId",
                table: "TurnosCaja");

            migrationBuilder.DropColumn(
                name: "TurnoCajaId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "TurnoCajaId",
                table: "MovimientosCaja");

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_IdUsuarioApertura",
                table: "TurnosCaja",
                column: "IdUsuarioApertura");

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_IdUsuarioCierre",
                table: "TurnosCaja",
                column: "IdUsuarioCierre");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdTurnoCaja",
                table: "Pagos",
                column: "IdTurnoCaja");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_IdTurnoCaja",
                table: "MovimientosCaja",
                column: "IdTurnoCaja");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosCaja_TurnosCaja_IdTurnoCaja",
                table: "MovimientosCaja",
                column: "IdTurnoCaja",
                principalTable: "TurnosCaja",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_TurnosCaja_IdTurnoCaja",
                table: "Pagos",
                column: "IdTurnoCaja",
                principalTable: "TurnosCaja",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TurnosCaja_Usuarios_IdUsuarioApertura",
                table: "TurnosCaja",
                column: "IdUsuarioApertura",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TurnosCaja_Usuarios_IdUsuarioCierre",
                table: "TurnosCaja",
                column: "IdUsuarioCierre",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosCaja_TurnosCaja_IdTurnoCaja",
                table: "MovimientosCaja");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_TurnosCaja_IdTurnoCaja",
                table: "Pagos");

            migrationBuilder.DropForeignKey(
                name: "FK_TurnosCaja_Usuarios_IdUsuarioApertura",
                table: "TurnosCaja");

            migrationBuilder.DropForeignKey(
                name: "FK_TurnosCaja_Usuarios_IdUsuarioCierre",
                table: "TurnosCaja");

            migrationBuilder.DropIndex(
                name: "IX_TurnosCaja_IdUsuarioApertura",
                table: "TurnosCaja");

            migrationBuilder.DropIndex(
                name: "IX_TurnosCaja_IdUsuarioCierre",
                table: "TurnosCaja");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_IdTurnoCaja",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_IdTurnoCaja",
                table: "MovimientosCaja");

            migrationBuilder.AddColumn<int>(
                name: "UsuarioAperturaId",
                table: "TurnosCaja",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioCierreId",
                table: "TurnosCaja",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TurnoCajaId",
                table: "Pagos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TurnoCajaId",
                table: "MovimientosCaja",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_UsuarioAperturaId",
                table: "TurnosCaja",
                column: "UsuarioAperturaId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_UsuarioCierreId",
                table: "TurnosCaja",
                column: "UsuarioCierreId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_TurnoCajaId",
                table: "Pagos",
                column: "TurnoCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_TurnoCajaId",
                table: "MovimientosCaja",
                column: "TurnoCajaId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosCaja_TurnosCaja_TurnoCajaId",
                table: "MovimientosCaja",
                column: "TurnoCajaId",
                principalTable: "TurnosCaja",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_TurnosCaja_TurnoCajaId",
                table: "Pagos",
                column: "TurnoCajaId",
                principalTable: "TurnosCaja",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TurnosCaja_Usuarios_UsuarioAperturaId",
                table: "TurnosCaja",
                column: "UsuarioAperturaId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TurnosCaja_Usuarios_UsuarioCierreId",
                table: "TurnosCaja",
                column: "UsuarioCierreId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }
    }
}

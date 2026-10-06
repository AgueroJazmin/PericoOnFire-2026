using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    /// <inheritdoc />
    public partial class Caja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdTurnoCaja",
                table: "Pagos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TurnoCajaId",
                table: "Pagos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdTurnoCaja",
                table: "MovimientosCaja",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TurnoCajaId",
                table: "MovimientosCaja",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TurnosCaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    IdUsuarioApertura = table.Column<int>(type: "integer", nullable: false),
                    FechaApertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MontoInicial = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IdUsuarioCierre = table.Column<int>(type: "integer", nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MontoEsperado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MontoContado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Diferencia = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ObservacionesCierre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    UsuarioAperturaId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioCierreId = table.Column<int>(type: "integer", nullable: true),
                    EstadoRegistro = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnosCaja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TurnosCaja_Usuarios_UsuarioAperturaId",
                        column: x => x.UsuarioAperturaId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TurnosCaja_Usuarios_UsuarioCierreId",
                        column: x => x.UsuarioCierreId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_TurnoCajaId",
                table: "Pagos",
                column: "TurnoCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_TurnoCajaId",
                table: "MovimientosCaja",
                column: "TurnoCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_UsuarioAperturaId",
                table: "TurnosCaja",
                column: "UsuarioAperturaId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_UsuarioCierreId",
                table: "TurnosCaja",
                column: "UsuarioCierreId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosCaja_TurnosCaja_TurnoCajaId",
                table: "MovimientosCaja");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_TurnosCaja_TurnoCajaId",
                table: "Pagos");

            migrationBuilder.DropTable(
                name: "TurnosCaja");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_TurnoCajaId",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_TurnoCajaId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "IdTurnoCaja",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "TurnoCajaId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "IdTurnoCaja",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "TurnoCajaId",
                table: "MovimientosCaja");
        }
    }
}

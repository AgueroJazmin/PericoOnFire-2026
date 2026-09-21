using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    /// <inheritdoc />
    public partial class Cambios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaProgramadaEntrega",
                table: "Pedidos");

            migrationBuilder.AddColumn<DateTime>(
                name: "HoraDeseada",
                table: "Comandas",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HoraDeseada",
                table: "Comandas");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaProgramadaEntrega",
                table: "Pedidos",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}

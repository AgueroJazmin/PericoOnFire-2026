using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    /// <inheritdoc />
    public partial class Emilse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsAdicional",
                table: "Pedidos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "IdComanda",
                table: "MovimientosCaja",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DevolucionPendiente",
                table: "Comandas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCancelacion",
                table: "Comandas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDevuelto",
                table: "Comandas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCancelacion",
                table: "Comandas",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsAdicional",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "IdComanda",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "DevolucionPendiente",
                table: "Comandas");

            migrationBuilder.DropColumn(
                name: "FechaCancelacion",
                table: "Comandas");

            migrationBuilder.DropColumn(
                name: "MontoDevuelto",
                table: "Comandas");

            migrationBuilder.DropColumn(
                name: "MotivoCancelacion",
                table: "Comandas");
        }
    }
}

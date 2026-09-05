using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    /// <inheritdoc />
    public partial class    Pedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCancelado",
                table: "Pedidos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCancelacion",
                table: "Pedidos",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaCancelado",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "MotivoCancelacion",
                table: "Pedidos");
        }
    }
}

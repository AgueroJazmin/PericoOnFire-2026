using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    /// <inheritdoc />
    public partial class Inci23 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Descuento",
                table: "Comandas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoPorcentaje",
                table: "Comandas",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Descuento",
                table: "Comandas");

            migrationBuilder.DropColumn(
                name: "DescuentoPorcentaje",
                table: "Comandas");
        }
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PericoOnFire_2026.BD.Datos;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    [DbContext(typeof(MiDbContext))]
    [Migration("20260904130000_AgregarCantidadComensales")]
    public partial class AgregarCantidadComensales : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CantidadComensales",
                table: "Comandas",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CantidadComensales",
                table: "Comandas");
        }
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PericoOnFire_2026.BD.Datos;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    [DbContext(typeof(MiDbContext))]
    [Migration("20260904160000_AgregarFotoSalon")]
    public partial class AgregarFotoSalon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionesSalon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Foto = table.Column<byte[]>(type: "bytea", nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NombreArchivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EstadoRegistro = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesSalon", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ConfiguracionesSalon");
        }
    }
}

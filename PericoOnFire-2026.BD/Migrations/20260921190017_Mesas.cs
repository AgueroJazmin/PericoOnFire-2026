using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    /// <inheritdoc />
    public partial class Mesas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Columna",
                table: "Mesas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Fila",
                table: "Mesas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Forma",
                table: "Mesas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IdSala",
                table: "Mesas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tamanio",
                table: "Mesas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Salas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    EstadoRegistro = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mesas_IdSala",
                table: "Mesas",
                column: "IdSala");

            migrationBuilder.AddForeignKey(
                name: "FK_Mesas_Salas_IdSala",
                table: "Mesas",
                column: "IdSala",
                principalTable: "Salas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mesas_Salas_IdSala",
                table: "Mesas");

            migrationBuilder.DropTable(
                name: "Salas");

            migrationBuilder.DropIndex(
                name: "IX_Mesas_IdSala",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "Columna",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "Fila",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "Forma",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "IdSala",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "Tamanio",
                table: "Mesas");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    /// <inheritdoc />
    public partial class Jaz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromocionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdProductoPromo = table.Column<int>(type: "integer", nullable: false),
                    IdProductoComponente = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    EstadoRegistro = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromocionItems_Productos_IdProductoComponente",
                        column: x => x.IdProductoComponente,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromocionItems_Productos_IdProductoPromo",
                        column: x => x.IdProductoPromo,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromocionItems_IdProductoComponente",
                table: "PromocionItems",
                column: "IdProductoComponente");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionItems_IdProductoPromo",
                table: "PromocionItems",
                column: "IdProductoPromo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromocionItems");
        }
    }
}

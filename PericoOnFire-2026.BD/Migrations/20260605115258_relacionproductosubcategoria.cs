using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    /// <inheritdoc />
    public partial class relacionproductosubcategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Subcategorias_SubcategoriaId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_SubcategoriaId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "SubcategoriaId",
                table: "Productos");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_IdSubcategoria",
                table: "Productos",
                column: "IdSubcategoria");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Subcategorias_IdSubcategoria",
                table: "Productos",
                column: "IdSubcategoria",
                principalTable: "Subcategorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Subcategorias_IdSubcategoria",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_IdSubcategoria",
                table: "Productos");

            migrationBuilder.AddColumn<int>(
                name: "SubcategoriaId",
                table: "Productos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_SubcategoriaId",
                table: "Productos",
                column: "SubcategoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Subcategorias_SubcategoriaId",
                table: "Productos",
                column: "SubcategoriaId",
                principalTable: "Subcategorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

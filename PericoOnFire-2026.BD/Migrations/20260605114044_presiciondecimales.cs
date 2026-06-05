using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PericoOnFire_2026.BD.Migrations
{
    /// <inheritdoc />
    public partial class presiciondecimales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subcategorias_Categorias_CategoriaId",
                table: "Subcategorias");

            migrationBuilder.DropIndex(
                name: "IX_Subcategorias_CategoriaId",
                table: "Subcategorias");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "Subcategorias");

            migrationBuilder.CreateIndex(
                name: "IX_Subcategorias_IdCategoria",
                table: "Subcategorias",
                column: "IdCategoria");

            migrationBuilder.AddForeignKey(
                name: "FK_Subcategorias_Categorias_IdCategoria",
                table: "Subcategorias",
                column: "IdCategoria",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subcategorias_Categorias_IdCategoria",
                table: "Subcategorias");

            migrationBuilder.DropIndex(
                name: "IX_Subcategorias_IdCategoria",
                table: "Subcategorias");

            migrationBuilder.AddColumn<int>(
                name: "CategoriaId",
                table: "Subcategorias",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Subcategorias_CategoriaId",
                table: "Subcategorias",
                column: "CategoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subcategorias_Categorias_CategoriaId",
                table: "Subcategorias",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

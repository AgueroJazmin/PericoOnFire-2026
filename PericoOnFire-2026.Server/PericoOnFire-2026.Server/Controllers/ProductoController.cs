using Microsoft.AspNetCore.Mvc;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;
using PericoOnFire_2026.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Producto")]
    public class ProductosController : ControllerBase
    {
        private readonly IProductoRepositorio repositorio;
        private readonly MiDbContext context;

        public ProductosController(IProductoRepositorio repositorio, MiDbContext context)
        {
            this.repositorio = repositorio;
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Producto>>> Get()
        {
            return await repositorio.Select();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductoCrearDTO>> GetById(int id)
        {
            var producto = await repositorio.SelectById(id);

            if (producto == null)
                return NotFound($"No existe el producto con id {id}.");

            var dto = new ProductoCrearDTO
            {
                Nombre = producto.Nombre,
                Precio = producto.Precio,
                IdSubcategoria = producto.IdSubcategoria,
                SectorDestino = producto.SectorDestino,
                Activo = producto.Activo
            };

            return Ok(dto);
        }

        [HttpGet("Activos")]
        public async Task<ActionResult<List<Producto>>> GetActivos()
        {
            return await repositorio.SelectActivos();
        }

        [HttpGet("Subcategoria/{idSubcategoria:int}")]
        public async Task<ActionResult<List<Producto>>> GetBySubcategoria(int idSubcategoria)
        {
            return await repositorio.SelectBySubcategoria(idSubcategoria);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(ProductoCrearDTO dto)
        {
            var producto = new Producto
            {
                Nombre = dto.Nombre,
                Precio = dto.Precio,
                IdSubcategoria = dto.IdSubcategoria,
                SectorDestino = dto.SectorDestino,
                Activo = dto.Activo
            };

            var id = await repositorio.Insert(producto);

            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, ProductoCrearDTO dto)
        {
            var producto = await repositorio.SelectById(id);

            if (producto == null)
                return NotFound($"No existe el producto con id {id}.");

            producto.Nombre = dto.Nombre;
            producto.Precio = dto.Precio;
            producto.IdSubcategoria = dto.IdSubcategoria;
            producto.SectorDestino = dto.SectorDestino;
            producto.Activo = dto.Activo;

            var resultado = await repositorio.Update(id, producto);

            if (!resultado)
                return BadRequest("No se pudo actualizar el producto.");

            return Ok("Producto actualizado correctamente.");
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var producto = await repositorio.SelectById(id);

            if (producto == null)
                return NotFound($"No existe el producto con id {id}.");

            var resultado = await repositorio.Delete(id);

            if (!resultado)
                return BadRequest("No se pudo eliminar el producto.");

            return Ok("Producto eliminado correctamente.");
        }
    }
}

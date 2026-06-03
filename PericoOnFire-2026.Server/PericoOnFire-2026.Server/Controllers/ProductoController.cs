using Microsoft.AspNetCore.Mvc;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Producto")]
    public class ProductosController : ControllerBase
    {
        private readonly IProductoRepositorio repositorio;

        public ProductosController(IProductoRepositorio repositorio)
        {
            this.repositorio = repositorio;
        }

        [HttpGet]
        public async Task<ActionResult<List<Producto>>> Get()
        {
            return await repositorio.Select();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Producto>> Get(int id)
        {
            var producto = await repositorio.SelectById(id);

            if (producto == null) return NotFound();

            return producto;
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
        public async Task<ActionResult<int>> Post(Producto producto)
        {
            var id = await repositorio.Insert(producto);
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, Producto producto)
        {
            var ok = await repositorio.Update(id, producto);

            if (!ok) return BadRequest();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var ok = await repositorio.Delete(id);

            if (!ok) return NotFound();

            return NoContent();
        }
    }
}

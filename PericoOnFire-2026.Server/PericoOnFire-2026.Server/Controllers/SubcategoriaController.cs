using Microsoft.AspNetCore.Mvc;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Subcategoria")]
    public class SubcategoriasController : ControllerBase
    {
        private readonly ISubcategoriaRepositorio repositorio;

        public SubcategoriasController(ISubcategoriaRepositorio repositorio)
        {
            this.repositorio = repositorio;
        }

        [HttpGet]
        public async Task<ActionResult<List<Subcategoria>>> Get()
        {
            return await repositorio.Select();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Subcategoria>> Get(int id)
        {
            var entidad = await repositorio.SelectById(id);

            if (entidad == null)
                return NotFound();

            return entidad;
        }

        [HttpGet("Categoria/{idCategoria:int}")]
        public async Task<ActionResult<List<Subcategoria>>> GetByCategoria(int idCategoria)
        {
            return await repositorio.SelectByCategoria(idCategoria);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(Subcategoria entidad)
        {
            var id = await repositorio.Insert(entidad);

            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, Subcategoria entidad)
        {
            var ok = await repositorio.Update(id, entidad);

            if (!ok)
                return BadRequest();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var ok = await repositorio.Delete(id);

            if (!ok)
                return NotFound();

            return NoContent();
        }
    }
}

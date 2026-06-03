using Microsoft.AspNetCore.Mvc;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;

namespace PericoOnFire_2026.Server.Controllers
{
    [Route("api/Categoria")]
    [ApiController]
    public class CategoriasController : ControllerBase
    {
        private readonly ICategoriaRepositorio repositorio;

        public CategoriasController(ICategoriaRepositorio repositorio)
        {
            this.repositorio = repositorio;
        }

        [HttpGet]
        public async Task<ActionResult<List<Categoria>>> Get()
        {
            return await repositorio.Select();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Categoria>> Get(int id)
        {
            var categoria = await repositorio.SelectById(id);

            if (categoria == null)
                return NotFound();

            return categoria;
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(Categoria categoria)
        {
            var id = await repositorio.Insert(categoria);
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, Categoria categoria)
        {
            var ok = await repositorio.Update(id, categoria);

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

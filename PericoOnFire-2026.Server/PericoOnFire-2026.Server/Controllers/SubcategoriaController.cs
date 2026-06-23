using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;
using PericoOnFire_2026.BD.Datos;


namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Subcategoria")]
    public class SubcategoriasController : ControllerBase
    {
        private readonly ISubcategoriaRepositorio repositorio;
        private readonly MiDbContext context;
        public SubcategoriasController(ISubcategoriaRepositorio repositorio, MiDbContext context)
        {
            this.repositorio = repositorio;
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Subcategoria>>> Get()
        {
            var lista = await context.Subcategorias
                .Include(s => s.Categoria)
                .Select(s => new SubcategoriaListadoDTO
                {
            Id = s.Id,
            IdCategoria = s.IdCategoria,
            NombreSubcategoria = s.NombreSubcategoria,
            NombreCategoria = s.Categoria.NombreCategoria
                })
                .ToListAsync();
            
            return Ok(lista);
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
        public async Task<ActionResult<int>> Post(SubcategoriaCrearDTO dto)
        {
            var existeCategoria = await context.Categorias
                .AnyAsync(c => c.Id == dto.IdCategoria);

            if (!existeCategoria)
            {
                return BadRequest($"No existe la categoría con Id {dto.IdCategoria}.");
            }

            var subcategoria = new Subcategoria
            {
                IdCategoria = dto.IdCategoria,
                NombreSubcategoria = dto.NombreSubcategoria,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            var id = await repositorio.Insert(subcategoria);

            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, SubcategoriaCrearDTO dto)
        {
            var subcategoria = await repositorio.SelectById(id);
            if (subcategoria == null)
                return NotFound();

            subcategoria.NombreSubcategoria = dto.NombreSubcategoria;
            subcategoria.IdCategoria = dto.IdCategoria;

            var ok = await repositorio.Update(id, subcategoria);
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

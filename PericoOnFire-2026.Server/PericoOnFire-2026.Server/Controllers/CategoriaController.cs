using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;
using PericoOnFire_2026.Servicio.ServicioHttp;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.Server.Controllers
{
    [Route("api/Categoria")]
    [ApiController]
    public class CategoriasController : ControllerBase
    {
        private readonly ICategoriaRepositorio repositorio;
        private readonly MiDbContext context;

        public CategoriasController(ICategoriaRepositorio repositorio, MiDbContext context)
        {
            this.repositorio = repositorio;
            this.context = context;
        }
        
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<CategoriaListadoDTO>>> Get()
        {
            var lista = await context.Categorias
                .Select(c => new CategoriaListadoDTO
                {
                    Id = c.Id,
                    NombreCategoria = c.NombreCategoria
                })
                .ToListAsync();
            
            return Ok(lista);
        }

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Categoria>> Get(int id)
        {
            var categoria = await repositorio.SelectById(id);

            if (categoria == null)
                return NotFound();

            return categoria;
        }
        [Authorize(Roles = "Administracion")]
        
        [HttpPost]
        public async Task<ActionResult<int>> Post(CategoriaCrearDTO dto)
        {
            var categoria = new Categoria
            {
                NombreCategoria = dto.NombreCategoria,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            var id = await repositorio.Insert(categoria);

            return Ok(id);
        }

        [Authorize(Roles = "Administracion")]
       
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, CategoriaCrearDTO dto)
        {
            var categoria = await repositorio.SelectById(id);
            if (categoria == null)
                return NotFound();

            categoria.NombreCategoria = dto.NombreCategoria;

            var ok = await repositorio.Update(id, categoria);
            if (!ok)
                return BadRequest();
            return NoContent();
        }

        [Authorize(Roles = "Administracion")]
        
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var ok = await repositorio.Delete(id);

                if (!ok)
                    return NotFound();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }
    }
}

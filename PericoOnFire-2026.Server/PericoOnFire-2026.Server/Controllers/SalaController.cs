using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Sala")]
    public class SalaController : ControllerBase
    {
        private readonly IRepositorio<Sala> repositorio;
        private readonly MiDbContext context;

        public SalaController(IRepositorio<Sala> repositorio, MiDbContext context)
        {
            this.repositorio = repositorio;
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<SalaDTO>>> Get()
        {
            var lista = await context.Salas
                .OrderBy(s => s.Orden)
                .ThenBy(s => s.Id)
                .Select(s => new SalaDTO
                {
                    Id = s.Id,
                    Nombre = s.Nombre,
                    Orden = s.Orden,
                    CantidadMesas = s.Mesas.Count
                })
                .ToListAsync();

            return Ok(lista);
        }

        [Authorize(Roles = "Administracion")]
        [HttpPost]
        public async Task<ActionResult<int>> Post(SalaCrearDTO dto)
        {
            var maxOrden = (await context.Salas.Select(s => (int?)s.Orden).MaxAsync()) ?? -1;

            var sala = new Sala
            {
                Nombre = dto.Nombre.Trim(),
                Orden = maxOrden + 1,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            var id = await repositorio.Insert(sala);
            return Ok(id);
        }

        [Authorize(Roles = "Administracion")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, SalaCrearDTO dto)
        {
            var sala = await repositorio.SelectById(id);
            if (sala == null)
                return NotFound();

            sala.Nombre = dto.Nombre.Trim();
            sala.Orden = dto.Orden;

            var resultado = await repositorio.Update(id, sala);
            if (!resultado)
                return BadRequest();

            return Ok();
        }

        // Solo se puede borrar una sala vacía. Si tiene mesas, la FK Restrict de
        // Mesa.IdSala hace que el repositorio genérico devuelva InvalidOperationException.
        [Authorize(Roles = "Administracion")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var resultado = await repositorio.Delete(id);
                if (!resultado)
                    return NotFound();

                return Ok();
            }
            catch (InvalidOperationException)
            {
                return Conflict(new { error = "No se puede eliminar la sala: todavía tiene mesas. Movela o eliminalas primero." });
            }
        }
    }
}
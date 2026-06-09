using Microsoft.AspNetCore.Mvc;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;
using Microsoft.EntityFrameworkCore;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Mesa")]
    public class MesasController : ControllerBase
    {
        private readonly IRepositorio<Mesa> repositorio;

        public MesasController(IRepositorio<Mesa> repositorio)
        {
            this.repositorio = repositorio;
        }

        [HttpGet]
        public async Task<ActionResult<List<Mesa>>> Get()
        {
            return await repositorio.Select();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Mesa>> Get(int id)
        {
            var mesa = await repositorio.SelectById(id);

            if (mesa == null)
                return NotFound();

            return mesa;
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(MesaDTO dto)
        {
            var mesa = new Mesa
            {
                NumeroMesa = dto.NumeroMesa,
                Estado = dto.Estado,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            var id = await repositorio.Insert(mesa);

            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, MesaDTO dto)
        {
            var mesa = await repositorio.SelectById(id);

            if (mesa == null)
                return NotFound();

            mesa.NumeroMesa = dto.NumeroMesa;
            mesa.Estado = dto.Estado;

            var resultado = await repositorio.Update(id, mesa);

            if (!resultado)
                return BadRequest();

            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var resultado = await repositorio.Delete(id);

            if (!resultado)
                return NotFound();

            return Ok();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.Repositorio.Repositorios;


namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Comanda")]
    public class ComandasController : ControllerBase
    {
        private readonly IComandaRepositorio repositorio;
        private readonly MiDbContext context;

        public ComandasController(IComandaRepositorio repositorio,
                                  MiDbContext context)
        {
            this.repositorio = repositorio;
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Comanda>>> Get()
        {
            return await repositorio.Select();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Comanda>> Get(int id)
        {
            var entidad = await repositorio.SelectById(id);

            if (entidad == null)
                return NotFound();

            return entidad;
        }

        [HttpGet("Abiertas")]
        public async Task<ActionResult<List<Comanda>>> GetAbiertas()
        {
            return await repositorio.SelectAbiertas();
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(ComandaCrearDTO dto)
        {
            var comanda = new Comanda
            {
                IdMesa = dto.IdMesa,
                IdCliente = dto.IdCliente,
                IdUsuario = dto.IdUsuario,
                TipoServicio = dto.TipoServicio,
                Observaciones = dto.Observaciones,
                Estado = EnumEstadoComanda.Abierta,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            var id = await repositorio.Insert(comanda);

            return Ok(id);
        }
    }
}

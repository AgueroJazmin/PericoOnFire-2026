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
    [Route("api/Pedido")]
    public class PedidosController : ControllerBase
    {
        private readonly IPedidoRepositorio repositorio;
        private readonly MiDbContext context;

        public PedidosController(IPedidoRepositorio repositorio,
                                  MiDbContext context)
        {
            this.repositorio = repositorio;
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Pedido>>> Get()
        {
            return await repositorio.Select();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Pedido>> Get(int id)
        {
            var pedido = await repositorio.SelectById(id);

            if (pedido == null)
                return NotFound();

            return pedido;
        }

        [HttpGet("Pendientes")]
        public async Task<ActionResult<List<Pedido>>> GetPendientes()
        {
            return await repositorio.SelectPendientes();
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(PedidoCrearDTO dto)
        {
            var pedido = new Pedido
            {
                IdComanda = dto.IdComanda,
                SectorDestino = dto.SectorDestino,
                Observaciones = dto.Observaciones,
                Estado = EnumEstadoPedido.Pendiente,
                FechaPedido = DateTime.Now,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            var id = await repositorio.Insert(pedido);

            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, PedidoCrearDTO dto)
        {
            var pedido = await repositorio.SelectById(id);

            if (pedido == null)
                return NotFound();

            pedido.IdComanda = dto.IdComanda;
            pedido.SectorDestino = dto.SectorDestino;
            pedido.Observaciones = dto.Observaciones;

            var resultado = await repositorio.Update(id, pedido);

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

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

        //Este endpoint obtiene los pedidos de un sector específico,
        //útil para que cada sector vea sus pedidos pendientes.
        [HttpGet("Comanda/{idComanda:int}")]
        public async Task<ActionResult<List<PedidoDTO>>> GetByComanda(int idComanda)
        {
            var pedidos = await repositorio.SelectByComanda(idComanda);
            return Ok(MapearPedidos(pedidos));
        }

        //Este endpoint obtiene los pedidos de un sector específico
        [HttpGet("Sector/{sector}")]
        public async Task<ActionResult<List<PedidoDTO>>> GetBySector(EnumSectorDestino sector)
        {
            var pedidos = await repositorio.SelectBySector(sector);
            return Ok(MapearPedidos(pedidos));
        }

        //Este endpoint permite cambiar el estado de un pedido específico.
        [HttpPut("{id:int}/Estado")]
        public async Task<ActionResult> CambiarEstado(int id, CambiarEstadoPedidoDTO dto)
        {
            var resultado = await repositorio.CambiarEstado(id, dto.Estado);

            if (!resultado)
                return NotFound();

            return Ok();
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

        //Nuevo endpoint que consume el mozo desde el panel lateral para cargar los ítems de un pedido agrupados por sector.
        [HttpPost("CargarItems")]
        public async Task<ActionResult<List<int>>> CargarItems(CargarPedidoDTO dto)
        {
            try
            {
                var idsPedidos = await repositorio.CargarPedidoAgrupado(dto.IdComanda, dto.Items);
                return Ok(idsPedidos);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
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

        //
        private List<PedidoDTO> MapearPedidos(List<Pedido> pedidos)
        {
            return pedidos.Select(p => new PedidoDTO
            {
                Id = p.Id,
                IdComanda = p.IdComanda,
                SectorDestino = p.SectorDestino,
                Estado = p.Estado,
                FechaPedido = p.FechaPedido,
                FechaInicioPreparacion = p.FechaInicioPreparacion,
                FechaListo = p.FechaListo,
                FechaEntregado = p.FechaEntregado,
                IdDelivery = p.IdDelivery,
                Observaciones = p.Observaciones,
                NumeroMesa = p.Comanda != null ? p.Comanda.Mesa?.NumeroMesa : null,
                TipoServicio = p.Comanda?.TipoServicio,
                DetallesPedido = p.DetallesPedido.Select(d => new DetallePedidoDTO
                {
                    Id = d.Id,
                    IdPedido = d.IdPedido,
                    IdProducto = d.IdProducto,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Observacion = d.Observacion,
                    NombreProducto = d.Producto.Nombre
                }).ToList()
            }).ToList();
        }
    }
}

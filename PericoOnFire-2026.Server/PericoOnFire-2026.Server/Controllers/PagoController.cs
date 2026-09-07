using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Pago")]
    public class PagosController : ControllerBase
    {
        private readonly MiDbContext context;

        public PagosController(MiDbContext context)
        {
            this.context = context;
        }

        //Cuentas que el mozo ya cerró (PendienteCobro) y están esperando que caja cobre.
        //OJO: el total se recalcula siempre desde los DetallesPedido reales y no desde
        //Comanda.Total, porque ese campo solo se carga una vez al confirmar la comanda
        //(ver ComandaController.Confirmar) y nunca se actualiza cuando el mozo agrega
        //productos en una ronda posterior con "Agregar productos". Confiar en Comanda.Total
        //acá mostraría un importe viejo, así que se ignora y se suma directo de los detalles.
        [HttpGet("PendientesCobro")]
        public async Task<ActionResult<List<ComandaPendienteCobroDTO>>> GetPendientesCobro()
        {
            var comandas = await context.Comandas
                .Include(c => c.Mesa)
                .Include(c => c.Usuario)
                .Include(c => c.Pedidos.Where(p => p.Estado != EnumEstadoPedido.Cancelado))
                    .ThenInclude(p => p.DetallesPedido)
                        .ThenInclude(d => d.Producto)
                .Where(c => c.Estado == EnumEstadoComanda.PendienteCobro)
                .OrderBy(c => c.FechaApertura)
                .Select(c => new ComandaPendienteCobroDTO
                {
                    Id = c.Id,
                    NumeroMesa = c.Mesa != null ? c.Mesa.NumeroMesa : null,
                    TipoServicio = c.TipoServicio,
                    FechaApertura = c.FechaApertura,
                    CantidadComensales = c.CantidadComensales,
                    NombreMozo = c.Usuario != null ? c.Usuario.Nombre : null,
                    Items = c.Pedidos
                        .Where(p => p.Estado != EnumEstadoPedido.Cancelado)
                        .SelectMany(p => p.DetallesPedido)
                        .Select(d => new ItemCuentaDTO
                        {
                            NombreProducto = d.Producto.Nombre,
                            Cantidad = d.Cantidad,
                            PrecioUnitario = d.PrecioUnitario
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(comandas);
        }

        //Acá es donde caja cobra y recién ahí libera la mesa: mientras la comanda siga
        //PendienteCobro, la mesa se mantiene bloqueada para que no la reasigne otro mozo
        //mientras el pago todavía no se concretó.
        [HttpPost("Registrar")]
        public async Task<ActionResult<PagoRegistradoDTO>> Registrar(RegistrarPagoDTO dto)
        {
            var comanda = await context.Comandas
                .Include(c => c.Pedidos.Where(p => p.Estado != EnumEstadoPedido.Cancelado))
                    .ThenInclude(p => p.DetallesPedido)
                .FirstOrDefaultAsync(c => c.Id == dto.IdComanda);

            if (comanda == null)
                return NotFound();

            if (comanda.Estado != EnumEstadoComanda.PendienteCobro)
                return Conflict("Esta comanda no está pendiente de cobro.");

            var total = comanda.Pedidos
                .Where(p => p.Estado != EnumEstadoPedido.Cancelado)
                .SelectMany(p => p.DetallesPedido)
                .Sum(d => d.Cantidad * d.PrecioUnitario);

            if (total <= 0)
                return Conflict("La comanda no tiene productos para cobrar.");

            if (dto.MontoPagado < total)
                return Conflict($"El monto pagado (${dto.MontoPagado:0.00}) es menor al total de la cuenta (${total:0.00}).");

            var pago = new Pago
            {
                IdComanda = comanda.Id,
                IdUsuarioCaja = dto.IdUsuarioCaja,
                TipoPago = dto.TipoPago,
                MontoTotal = total,
                MontoPagado = dto.MontoPagado,
                Vuelto = dto.MontoPagado - total,
                FechaPago = DateTime.UtcNow,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            context.Pagos.Add(pago);

            comanda.Estado = EnumEstadoComanda.Pagada;
            comanda.Total = total;
            comanda.FechaCierre = DateTime.UtcNow;

            if (comanda.IdMesa.HasValue)
            {
                var mesa = await context.Mesas.FindAsync(comanda.IdMesa.Value);
                if (mesa != null)
                    mesa.Estado = EnumEstadoMesa.Libre;
            }

            await context.SaveChangesAsync();

            return Ok(new PagoRegistradoDTO
            {
                IdPago = pago.Id,
                MontoTotal = total,
                Vuelto = pago.Vuelto
            });
        }

        //Cubre el caso de una comanda que llegó a PendienteCobro sin nada para cobrar
        //(por ej. se cancelaron todos los pedidos, o quedó de antes de tener la validación
        //de "no hay pedidos sin entregar" en ComandaController.Cerrar). No tiene sentido
        //registrar un Pago de $0 -de hecho Pago.MontoTotal exige > 0-, así que acá directamente
        //se cierra la comanda como Cancelada y se libera la mesa, sin pasar por Registrar.
        [HttpPut("CerrarSinCobro/{idComanda:int}")]
        public async Task<ActionResult> CerrarSinCobro(int idComanda)
        {
            var comanda = await context.Comandas
                .Include(c => c.Pedidos.Where(p => p.Estado != EnumEstadoPedido.Cancelado))
                    .ThenInclude(p => p.DetallesPedido)
                .FirstOrDefaultAsync(c => c.Id == idComanda);

            if (comanda == null)
                return NotFound();

            if (comanda.Estado != EnumEstadoComanda.PendienteCobro)
                return Conflict("Esta comanda no está pendiente de cobro.");

            var total = comanda.Pedidos
                .Where(p => p.Estado != EnumEstadoPedido.Cancelado)
                .SelectMany(p => p.DetallesPedido)
                .Sum(d => d.Cantidad * d.PrecioUnitario);

            if (total > 0)
                return Conflict("Esta comanda tiene productos cargados, no se puede cerrar sin cobro.");

            comanda.Estado = EnumEstadoComanda.Cancelada;
            comanda.FechaCierre = DateTime.UtcNow;

            if (comanda.IdMesa.HasValue)
            {
                var mesa = await context.Mesas.FindAsync(comanda.IdMesa.Value);
                if (mesa != null)
                    mesa.Estado = EnumEstadoMesa.Libre;
            }

            await context.SaveChangesAsync();
            return Ok();
        }
    }
}
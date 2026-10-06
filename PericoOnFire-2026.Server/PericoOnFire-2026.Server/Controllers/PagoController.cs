using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;
using PericoOnFire_2026.Server.Servicios;
using PericoOnFire_2026.Shared.Utilidades;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Pago")]
    [Authorize(Roles = "Caja,Administracion")]
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

            var numerosDiarios = await NumeroComandaDiario.ObtenerVariosAsync(
                context,
                comandas.Select(c => (c.Id, c.FechaApertura)));

            foreach (var comanda in comandas)
                comanda.NumeroDiario = numerosDiarios.GetValueOrDefault(comanda.Id);

            return Ok(comandas);
        }

        //Acá es donde caja cobra y recién ahí libera la mesa: mientras la comanda siga
        //PendienteCobro, la mesa se mantiene bloqueada para que no la reasigne otro mozo
        //mientras el pago todavía no se concretó.
        //Se puede cobrar con varias formas de pago a la vez (pago combinado): cada una queda como
        //una fila de Pago y las reglas (el vuelto solo sale del efectivo, etc.) viven en
        //CobroCombinado, que el cliente también usa para avisar antes de enviar.
        //Solo se cobra con la caja abierta, porque cada pago queda atado al turno para poder
        //hacer después el arqueo.
        [HttpPost("Registrar")]
        public async Task<ActionResult<PagoRegistradoDTO>> Registrar(RegistrarPagoDTO dto)
        {
            var turno = await CajaActual.ObtenerTurnoAbiertoAsync(context);
            if (turno == null)
                return Conflict("La caja está cerrada. Abrí un turno antes de cobrar.");

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

            var cobro = CobroCombinado.Evaluar(total, dto.Lineas);
            if (!cobro.EsValido)
                return Conflict(cobro.Error);

            //MontoTotal es el de la cuenta en todas las filas; el vuelto se carga solo en la de efectivo.
            var ahora = DateTime.UtcNow;
            var pagos = cobro.Lineas
                .Select(l => new Pago
                {
                    IdComanda = comanda.Id,
                    IdUsuarioCaja = dto.IdUsuarioCaja,
                    IdTurnoCaja = turno.Id,
                    TipoPago = l.TipoPago,
                    MontoTotal = total,
                    MontoPagado = l.Monto,
                    Vuelto = l.TipoPago == EnumTipoPago.Efectivo ? cobro.Vuelto : 0,
                    FechaPago = ahora,
                    EstadoRegistro = EnumEstadoRegistro.activo
                })
                .ToList();

            context.Pagos.AddRange(pagos);

            comanda.Estado = EnumEstadoComanda.Pagada;
            comanda.Total = total;
            comanda.FechaCierre = ahora;

            if (comanda.IdMesa.HasValue)
            {
                var mesa = await context.Mesas.FindAsync(comanda.IdMesa.Value);
                if (mesa != null)
                    mesa.Estado = EnumEstadoMesa.Libre;
            }

            await context.SaveChangesAsync();

            return Ok(new PagoRegistradoDTO
            {
                IdPago = pagos[0].Id,
                MontoTotal = total,
                Vuelto = cobro.Vuelto
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
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
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
        private readonly ILogger<PagosController> logger;

        public PagosController(MiDbContext context, ILogger<PagosController> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        //Cuentas que el mozo ya cerró PendienteCobro y están esperando que caja cobre.
        //Ojo que en el total se recalcula siempre desde los DetallesPedido reales y no desde
        //Comanda.Total, porque ese campo solo se carga una vez al confirmar la comanda
        //y nunca se actualiza cuando el mozo agrega productos en una ronda posterior con "Agregar productos"
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
                    Descuento = c.Descuento,
                    YaPagado = c.Pagos.Where(pg => pg.EstadoRegistro == EnumEstadoRegistro.activo)
                        .Sum(pg => pg.MontoPagado - pg.Vuelto) - c.MontoDevuelto,
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

        //Acá es donde caja cobra y recién ahí libera la mesa mientras la comanda siga
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
            try
            {
                return await OperacionAtomica.EjecutarAsync<ActionResult<PagoRegistradoDTO>>(context, async () =>
                {
                    var cuenta = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var cajero = await context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.IdApplicationUser == cuenta && u.Activo);
                    if (cajero == null) return Conflict("Tu cuenta no tiene un empleado activo asociado. Revisá la configuración del usuario antes de cobrar.");
                    var turno = await CajaActual.ObtenerTurnoAbiertoAsync(context);
                    if (turno == null)
                        return Conflict("La caja está cerrada. Abrí un turno antes de cobrar.");

                    var comanda = await context.Comandas
                        .Include(c => c.Pedidos.Where(p => p.Estado != EnumEstadoPedido.Cancelado))
                            .ThenInclude(p => p.DetallesPedido)
                        .FirstOrDefaultAsync(c => c.Id == dto.IdComanda);

                    if (comanda == null)
                        return NotFound();

                    if (comanda.Estado == EnumEstadoComanda.Pagada)
                        return Conflict("Esta comanda ya tiene un cobro registrado. Actualizá Caja para ver su estado.");

                    if (comanda.Estado != EnumEstadoComanda.PendienteCobro)
                        return Conflict("Esta comanda no está pendiente de cobro.");

                    var subtotal = comanda.Pedidos
                        .Where(p => p.Estado != EnumEstadoPedido.Cancelado)
                        .SelectMany(p => p.DetallesPedido)
                        .Sum(d => d.Cantidad * d.PrecioUnitario);

                    if (subtotal <= 0)
                        return Conflict("La comanda no tiene productos para cobrar.");

                    // Si el pedido ya tenía un cobro (se editó después de pagarlo), solo se cobra el saldo
                    // con el descuento que quedó guardado al editarlo.
                    var tuvoCobro = await context.Pagos.AnyAsync(p => p.IdComanda == comanda.Id);
                    var yaPagado = tuvoCobro
                        ? await context.Pagos
                            .Where(p => p.IdComanda == comanda.Id && p.EstadoRegistro == EnumEstadoRegistro.activo)
                            .SumAsync(p => p.MontoPagado - p.Vuelto) - comanda.MontoDevuelto
                        : 0m;

                    decimal descuento;
                    if (tuvoCobro)
                    {
                        descuento = comanda.Descuento;
                    }
                    else
                    {
                        descuento = CalculoDescuento.Calcular(subtotal, dto.TipoDescuento, dto.ValorDescuento, out var errorDescuento);
                        if (errorDescuento != null)
                            return Conflict(errorDescuento);
                    }

                    var total = subtotal - descuento;
                    var aCobrar = total - yaPagado;
                    if (aCobrar <= 0)
                        return Conflict("No hay saldo para cobrar en esta comanda. Actualizá Caja.");

                    var cobro = CobroCombinado.Evaluar(aCobrar, dto.Lineas);
                    if (!cobro.EsValido)
                        return Conflict(cobro.Error);

                    //MontoTotal es el de la cuenta en todas las filas; el vuelto se carga solo en la de efectivo.
                    var ahora = DateTime.UtcNow;
                    var pagos = cobro.Lineas
                        .Select(l => new Pago
                        {
                            IdComanda = comanda.Id,
                            IdUsuarioCaja = cajero.Id,
                            IdTurnoCaja = turno.Id,
                            TipoPago = l.TipoPago,
                            MontoTotal = aCobrar,
                            MontoPagado = l.Monto,
                            Vuelto = l.TipoPago == EnumTipoPago.Efectivo ? cobro.Vuelto : 0,
                            FechaPago = ahora,
                            EstadoRegistro = EnumEstadoRegistro.activo
                        })
                        .ToList();

                    context.Pagos.AddRange(pagos);

                    comanda.Estado = EnumEstadoComanda.Pagada;
                    comanda.Total = total;
                    if (!tuvoCobro)
                    {
                        comanda.Descuento = descuento;
                        comanda.DescuentoPorcentaje = dto.TipoDescuento == EnumTipoDescuento.Porcentaje ? dto.ValorDescuento : null;
                    }
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
                        MontoTotal = aCobrar,
                        Vuelto = cobro.Vuelto
                    });

                });
            }
            catch (Exception ex) when (OperacionAtomica.EsConflictoConcurrente(ex))
            {
                logger.LogWarning(ex, "Conflicto concurrente al cobrar comanda {IdComanda} tras los reintentos", dto.IdComanda);
                return Conflict("Otra operación está modificando esta cuenta. Actualizá Caja y verificá su estado antes de volver a cobrar.");
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error al guardar el cobro de comanda {IdComanda}", dto.IdComanda);
                return Conflict("No se pudo guardar el cobro. Actualizá Caja y comprobá si figura pagada antes de reintentar. El detalle quedó registrado en el servidor.");
            }
        }

        // Comandas a las que hay que devolverles plata, el pedido ya estaba pagado y se modificó o canceló
        [HttpGet("DevolucionesPendientes")]
        public async Task<ActionResult<List<DevolucionPendienteDTO>>> GetDevolucionesPendientes()
        {
            var comandas = await context.Comandas
                .Include(c => c.Cliente)
                .Where(c => c.DevolucionPendiente > 0)
                .OrderBy(c => c.FechaApertura)
                .ToListAsync();

            var numerosDiarios = await NumeroComandaDiario.ObtenerVariosAsync(
                context,
                comandas.Select(c => (c.Id, c.FechaApertura)));

            return Ok(comandas.Select(c => new DevolucionPendienteDTO
            {
                IdComanda = c.Id,
                NumeroDiario = numerosDiarios.GetValueOrDefault(c.Id),
                TipoServicio = c.TipoServicio,
                NombreCliente = c.Cliente?.Nombre,
                Monto = c.DevolucionPendiente,
                Motivo = c.Estado == EnumEstadoComanda.Cancelada
                    ? $"Pedido cancelado: {c.MotivoCancelacion}"
                    : "Se modificó el pedido y el total bajó"
            }).ToList());
        }

        // Registra la devolución como un movimiento de la caja abierta. En efectivo es un egreso (baja el
        // efectivo del cajón), con cualquier otro medio queda registrada sin mover el efectivo.
        [HttpPost("Devolucion")]
        public async Task<ActionResult> RegistrarDevolucion(RegistrarDevolucionDTO dto)
        {
            return await OperacionAtomica.EjecutarAsync<ActionResult>(context, async () =>
            {
                var cuenta = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cajero = await context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.IdApplicationUser == cuenta && u.Activo);
                if (cajero == null)
                    return Conflict("Tu cuenta no tiene un empleado activo asociado. Revisá la configuración del usuario.");

                var turno = await CajaActual.ObtenerTurnoAbiertoAsync(context);
                if (turno == null)
                    return Conflict("La caja está cerrada. Abrí un turno antes de registrar la devolución.");

                var comanda = await context.Comandas.FirstOrDefaultAsync(c => c.Id == dto.IdComanda);
                if (comanda == null) return NotFound();
                if (comanda.DevolucionPendiente <= 0)
                    return Conflict("Esta comanda no tiene devoluciones pendientes. Actualizá Caja.");

                var monto = comanda.DevolucionPendiente;
                var esEfectivo = dto.TipoPago == EnumTipoPago.Efectivo;

                if (esEfectivo)
                {
                    var cobrosEfectivo = await context.Pagos
                        .Where(p => p.IdTurnoCaja == turno.Id && p.EstadoRegistro == EnumEstadoRegistro.activo &&
                                    p.TipoPago == EnumTipoPago.Efectivo)
                        .SumAsync(p => p.MontoPagado - p.Vuelto);
                    var movimientos = await context.MovimientosCaja
                        .Where(m => m.IdTurnoCaja == turno.Id && m.EstadoRegistro == EnumEstadoRegistro.activo)
                        .Select(m => new { m.TipoMovimiento, m.Monto })
                        .ToListAsync();
                    decimal Movido(EnumTipoMovCaja tipo) => movimientos.Where(m => m.TipoMovimiento == tipo).Sum(m => m.Monto);
                    var efectivoEsperado = turno.MontoInicial + cobrosEfectivo +
                                           Movido(EnumTipoMovCaja.Ingreso) - Movido(EnumTipoMovCaja.Egreso) - Movido(EnumTipoMovCaja.Retiro);
                    if (monto > efectivoEsperado)
                        return Conflict($"No hay suficiente efectivo en caja: hay ${efectivoEsperado:0.00} y hay que devolver ${monto:0.00}. Elegí otro medio de devolución.");
                }

                var numero = (await NumeroComandaDiario.ObtenerVariosAsync(
                    context, new[] { (comanda.Id, comanda.FechaApertura) })).GetValueOrDefault(comanda.Id);

                context.MovimientosCaja.Add(new MovimientoCaja
                {
                    IdUsuario = cajero.Id,
                    IdTurnoCaja = turno.Id,
                    IdComanda = comanda.Id,
                    TipoMovimiento = esEfectivo ? EnumTipoMovCaja.Egreso : EnumTipoMovCaja.DevolucionSinEfectivo,
                    Monto = monto,
                    Motivo = $"Devolución COM-{numero:D3} ({dto.TipoPago})",
                    Observaciones = comanda.MotivoCancelacion,
                    FechaMovimiento = DateTime.UtcNow,
                    EstadoRegistro = EnumEstadoRegistro.activo
                });

                comanda.MontoDevuelto += monto;
                comanda.DevolucionPendiente = 0;

                await context.SaveChangesAsync();
                return Ok();
            });
        }

        //Cubre el caso de una comanda que llegó a PendienteCobro sin nada para cobrar
        //por ejemplo si se cancelaron todos los pedidos, o quedó de antes de tener la validación
        //de "no hay pedidos sin entregar" en ComandaController.Cerrar. 
        //No tiene sentido registrar un Pago de $0 -de hecho Pago.MontoTotal exige > 0-, así que acá directamente
        //se cierra la comanda como Cancelada y se libera la mesa, sin pasar por Registrar.
        [HttpPut("CerrarSinCobro/{idComanda:int}")]
        public async Task<ActionResult> CerrarSinCobro(int idComanda)
        {
            return await OperacionAtomica.EjecutarAsync<ActionResult>(context, async () =>
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

            });
        }
    }
}
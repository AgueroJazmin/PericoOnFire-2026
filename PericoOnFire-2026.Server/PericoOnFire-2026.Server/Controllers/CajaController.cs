using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Server.Servicios;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;
using PericoOnFire_2026.Shared.Utilidades;

namespace PericoOnFire_2026.Server.Controllers
{
    //Todo lo de caja que no es cobrar una cuenta (eso sigue en PagoController): estado de la caja,
    //apertura y cierre de turno con su arqueo, movimientos de dinero, ventas y estadísticas.
    //Los errores de negocio devuelven Conflict con un mensaje, porque el cliente solo muestra el
    //texto del server en ese caso.
    [ApiController]
    [Route("api/Caja")]
    [Authorize(Roles = "Caja,Administracion")]
    public class CajaController : ControllerBase
    {
        private const decimal MontoMaximo = 999_999_999m;

        private readonly MiDbContext context;
        private readonly UserManager<ApplicationUser> userManager;

        public CajaController(MiDbContext context, UserManager<ApplicationUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        //Con la caja cerrada también devuelve el efectivo contado en el último cierre,
        //para que al abrir el nuevo turno el monto inicial ya aparezca cargado.
        [HttpGet("Estado")]
        public async Task<ActionResult<EstadoCajaDTO>> GetEstado()
        {
            var turno = await CajaActual.ObtenerTurnoAbiertoAsync(context);

            if (turno == null)
            {
                var ultimoContado = await context.TurnosCaja
                    .Where(t => t.Estado == EnumEstadoTurnoCaja.Cerrado)
                    .OrderByDescending(t => t.FechaCierre)
                    .Select(t => t.MontoContado)
                    .FirstOrDefaultAsync();

                return Ok(new EstadoCajaDTO { Abierta = false, MontoInicialSugerido = ultimoContado });
            }

            return Ok(await ArmarEstadoAsync(turno));
        }

        [HttpPost("Abrir")]
        public async Task<ActionResult<TurnoCajaDTO>> Abrir(AbrirCajaDTO dto)
        {
            var montoInicial = Math.Round(dto.MontoInicial, 2);
            if (montoInicial < 0)
                return Conflict("El monto inicial no puede ser negativo.");
            if (montoInicial > MontoMaximo)
                return Conflict("El monto inicial es demasiado grande.");

            var usuario = await ObtenerUsuarioActualAsync();
            if (usuario == null)
                return Conflict("No se encontró un usuario de negocio asociado a esta cuenta.");

            if (await CajaActual.ObtenerTurnoAbiertoAsync(context) != null)
                return Conflict("Ya hay una caja abierta. Cerrá el turno actual antes de abrir otro.");

            var turno = new TurnoCaja
            {
                Estado = EnumEstadoTurnoCaja.Abierto,
                IdUsuarioApertura = usuario.Id,
                FechaApertura = DateTime.UtcNow,
                MontoInicial = montoInicial,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            context.TurnosCaja.Add(turno);

            //Se deja la apertura escrita en la lista de movimientos, así el turno se lee completo de corrido.
            context.MovimientosCaja.Add(new MovimientoCaja
            {
                IdUsuario = usuario.Id,
                TurnoCaja = turno,
                TipoMovimiento = EnumTipoMovCaja.Apertura,
                Monto = montoInicial,
                Motivo = "Apertura de caja",
                FechaMovimiento = turno.FechaApertura,
                EstadoRegistro = EnumEstadoRegistro.activo
            });

            await context.SaveChangesAsync();

            return Ok(ArmarTurnoDTO(turno, usuario.Nombre, null));
        }

        //Este es el arqueo: se compara el efectivo que el sistema espera en el cajón con el que
        //se contó de verdad. La diferencia (sobrante o faltante) queda guardada en el turno.
        [HttpPost("Cerrar")]
        public async Task<ActionResult<TurnoCajaDTO>> Cerrar(CerrarCajaDTO dto)
        {
            var montoContado = Math.Round(dto.MontoContado, 2);
            if (montoContado < 0)
                return Conflict("El efectivo contado no puede ser negativo.");
            if (montoContado > MontoMaximo)
                return Conflict("El efectivo contado es demasiado grande.");

            var observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim();
            if (observaciones != null && observaciones.Length > 300)
                return Conflict("Las observaciones no pueden superar los 300 caracteres.");

            var turno = await CajaActual.ObtenerTurnoAbiertoAsync(context);
            if (turno == null)
                return Conflict("No hay ninguna caja abierta.");

            var usuario = await ObtenerUsuarioActualAsync();
            if (usuario == null)
                return Conflict("No se encontró un usuario de negocio asociado a esta cuenta.");

            var estado = await ArmarEstadoAsync(turno);
            var esperado = estado.EfectivoEsperado;
            var diferencia = montoContado - esperado;
            var ahora = DateTime.UtcNow;

            turno.Estado = EnumEstadoTurnoCaja.Cerrado;
            turno.FechaCierre = ahora;
            turno.IdUsuarioCierre = usuario.Id;
            turno.MontoEsperado = esperado;
            turno.MontoContado = montoContado;
            turno.Diferencia = diferencia;
            turno.ObservacionesCierre = observaciones;

            context.MovimientosCaja.Add(new MovimientoCaja
            {
                IdUsuario = usuario.Id,
                IdTurnoCaja = turno.Id,
                TipoMovimiento = EnumTipoMovCaja.Cierre,
                Monto = montoContado,
                Motivo = "Cierre de caja",
                Observaciones = $"Esperado ${esperado:0.00} · Contado ${montoContado:0.00} · Diferencia ${diferencia:0.00}",
                FechaMovimiento = ahora,
                EstadoRegistro = EnumEstadoRegistro.activo
            });

            await context.SaveChangesAsync();

            return Ok(ArmarTurnoDTO(turno, estado.Turno?.NombreUsuarioApertura, usuario.Nombre));
        }

        //Los arqueos ya hechos, del más nuevo al más viejo.
        [HttpGet("Turnos")]
        public async Task<ActionResult<List<TurnoCajaDTO>>> GetTurnos()
        {
            var turnos = await context.TurnosCaja
                .Where(t => t.Estado == EnumEstadoTurnoCaja.Cerrado && t.EstadoRegistro == EnumEstadoRegistro.activo)
                .OrderByDescending(t => t.FechaCierre)
                .Take(30)
                .Select(t => new TurnoCajaDTO
                {
                    Id = t.Id,
                    Estado = t.Estado,
                    FechaApertura = t.FechaApertura,
                    NombreUsuarioApertura = t.UsuarioApertura.Nombre,
                    MontoInicial = t.MontoInicial,
                    FechaCierre = t.FechaCierre,
                    NombreUsuarioCierre = t.UsuarioCierre != null ? t.UsuarioCierre.Nombre : null,
                    MontoEsperado = t.MontoEsperado,
                    MontoContado = t.MontoContado,
                    Diferencia = t.Diferencia,
                    ObservacionesCierre = t.ObservacionesCierre
                })
                .ToListAsync();

            return Ok(turnos);
        }

        //Movimientos de un turno (por defecto el abierto). Incluye la apertura y, en los turnos
        //cerrados, el cierre. Los cobros no están acá: se ven en Ventas.
        [HttpGet("Movimientos")]
        public async Task<ActionResult<List<MovimientoCajaDTO>>> GetMovimientos([FromQuery] int? idTurno)
        {
            var idTurnoBuscado = idTurno ?? (await CajaActual.ObtenerTurnoAbiertoAsync(context))?.Id;
            if (idTurnoBuscado == null)
                return Ok(new List<MovimientoCajaDTO>());

            var movimientos = await context.MovimientosCaja
                .Where(m => m.IdTurnoCaja == idTurnoBuscado && m.EstadoRegistro == EnumEstadoRegistro.activo)
                .OrderByDescending(m => m.FechaMovimiento)
                .ThenByDescending(m => m.Id)
                .Select(m => new MovimientoCajaDTO
                {
                    Id = m.Id,
                    FechaMovimiento = m.FechaMovimiento,
                    TipoMovimiento = m.TipoMovimiento,
                    Monto = m.Monto,
                    Motivo = m.Motivo,
                    Observaciones = m.Observaciones,
                    NombreUsuario = m.Usuario.Nombre
                })
                .ToListAsync();

            return Ok(movimientos);
        }

        //Ingresos, egresos y retiros manuales. Apertura y cierre los escribe el sistema solo,
        //y el ajuste no se ofrece porque MovimientoCaja no admite montos negativos.
        [HttpPost("Movimientos")]
        public async Task<ActionResult<MovimientoCajaDTO>> PostMovimiento(MovimientoCajaCrearDTO dto)
        {
            var turno = await CajaActual.ObtenerTurnoAbiertoAsync(context);
            if (turno == null)
                return Conflict("La caja está cerrada. Abrí un turno para registrar movimientos.");

            if (dto.TipoMovimiento is not (EnumTipoMovCaja.Ingreso or EnumTipoMovCaja.Egreso or EnumTipoMovCaja.Retiro))
                return Conflict("Solo se pueden registrar ingresos, egresos o retiros.");

            var monto = Math.Round(dto.Monto, 2);
            if (monto <= 0)
                return Conflict("El monto debe ser mayor a cero.");
            if (monto > MontoMaximo)
                return Conflict("El monto es demasiado grande.");

            var motivo = (dto.Motivo ?? string.Empty).Trim();
            if (motivo.Length == 0)
                return Conflict("El motivo es obligatorio.");
            if (motivo.Length > 200)
                return Conflict("El motivo no puede superar los 200 caracteres.");

            var observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim();
            if (observaciones != null && observaciones.Length > 300)
                return Conflict("Las observaciones no pueden superar los 300 caracteres.");

            var usuario = await ObtenerUsuarioActualAsync();
            if (usuario == null)
                return Conflict("No se encontró un usuario de negocio asociado a esta cuenta.");

            if (dto.TipoMovimiento is EnumTipoMovCaja.Egreso or EnumTipoMovCaja.Retiro)
            {
                var estado = await ArmarEstadoAsync(turno);
                if (monto > estado.EfectivoEsperado)
                    return Conflict($"No hay suficiente efectivo en caja: hay ${estado.EfectivoEsperado:0.00} y querés sacar ${monto:0.00}.");
            }

            var movimiento = new MovimientoCaja
            {
                IdUsuario = usuario.Id,
                IdTurnoCaja = turno.Id,
                TipoMovimiento = dto.TipoMovimiento,
                Monto = monto,
                Motivo = motivo,
                Observaciones = observaciones,
                FechaMovimiento = DateTime.UtcNow,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            context.MovimientosCaja.Add(movimiento);
            await context.SaveChangesAsync();

            return Ok(new MovimientoCajaDTO
            {
                Id = movimiento.Id,
                FechaMovimiento = movimiento.FechaMovimiento,
                TipoMovimiento = movimiento.TipoMovimiento,
                Monto = movimiento.Monto,
                Motivo = movimiento.Motivo,
                Observaciones = movimiento.Observaciones,
                NombreUsuario = usuario.Nombre
            });
        }

        //Ventas cobradas entre dos fechas (días locales de Argentina, ambos incluidos).
        //Sin fechas devuelve las de hoy. Con todo=true devuelve todas las ventas hasta hoy.
        [HttpGet("Ventas")]
        public async Task<ActionResult<List<VentaCajaDTO>>> GetVentas([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] bool todo = false)
        {
            var rango = await ArmarRangoAsync(desde, hasta, todo);
            if (rango == null)
                return Conflict("El rango de fechas no puede superar un año.");

            return Ok(await ObtenerVentasAsync(rango.Value.InicioUtc, rango.Value.FinUtc));
        }

        //Las estadísticas salen de los pagos registrados (Pago.MontoPagado - Vuelto), no de
        //Comanda.Total, que puede quedar desactualizado si el mozo agregó rondas después de confirmar.
        [HttpGet("Estadisticas")]
        public async Task<ActionResult<ResumenVentasDTO>> GetEstadisticas([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] bool todo = false)
        {
            var rango = await ArmarRangoAsync(desde, hasta, todo);
            if (rango == null)
                return Conflict("El rango de fechas no puede superar un año.");

            var ventas = await ObtenerVentasAsync(rango.Value.InicioUtc, rango.Value.FinUtc);

            var resumen = new ResumenVentasDTO
            {
                Desde = rango.Value.Desde,
                Hasta = rango.Value.Hasta,
                TotalVendido = ventas.Sum(v => v.Total),
                CantidadVentas = ventas.Count
            };

            resumen.PorTipoPago = ventas
                .SelectMany(v => v.Pagos)
                .GroupBy(l => l.TipoPago)
                .Select(g => new TotalPorConceptoDTO
                {
                    Concepto = TiposPagoCaja.Nombre(g.Key),
                    Cantidad = g.Count(),
                    Monto = g.Sum(l => l.Monto)
                })
                .OrderByDescending(x => x.Monto)
                .ToList();

            resumen.PorTipoServicio = ventas
                .GroupBy(v => v.TipoServicio)
                .Select(g => new TotalPorConceptoDTO
                {
                    Concepto = NombreServicio(g.Key),
                    Cantidad = g.Count(),
                    Monto = g.Sum(v => v.Total)
                })
                .OrderByDescending(x => x.Monto)
                .ToList();

            resumen.PorDia = ventas
                .GroupBy(v => HoraArgentina.DesdeUtc(v.FechaPago).Date)
                .OrderBy(g => g.Key)
                .Select(g => new TotalPorConceptoDTO
                {
                    Concepto = g.Key.ToString("dd/MM/yyyy"),
                    Cantidad = g.Count(),
                    Monto = g.Sum(v => v.Total)
                })
                .ToList();

            var idsComandas = ventas.Select(v => v.IdComanda).ToList();
            if (idsComandas.Any())
            {
                var detalles = await context.DetallesPedido
                    .Where(d => idsComandas.Contains(d.Pedido.IdComanda) && d.Pedido.Estado != EnumEstadoPedido.Cancelado)
                    .Select(d => new { Nombre = d.Producto.Nombre, d.Cantidad, d.PrecioUnitario })
                    .ToListAsync();

                resumen.ProductosMasVendidos = detalles
                    .GroupBy(d => d.Nombre)
                    .Select(g => new TotalPorConceptoDTO
                    {
                        Concepto = g.Key,
                        Cantidad = g.Sum(d => d.Cantidad),
                        Monto = g.Sum(d => d.Cantidad * d.PrecioUnitario)
                    })
                    .OrderByDescending(x => x.Cantidad)
                    .ThenByDescending(x => x.Monto)
                    .Take(5)
                    .ToList();
            }

            return Ok(resumen);
        }

        // ---- internos ----

        private async Task<Usuario?> ObtenerUsuarioActualAsync()
        {
            var idApplicationUser = userManager.GetUserId(User);
            if (idApplicationUser == null)
                return null;

            return await context.Usuarios.FirstOrDefaultAsync(u => u.IdApplicationUser == idApplicationUser);
        }

        //Lo cobrado en efectivo es lo que quedó aplicado a la cuenta (MontoPagado - Vuelto): el
        //vuelto sale del cajón, así que no cuenta. Transferencia y QR no tocan el cajón.
        private async Task<EstadoCajaDTO> ArmarEstadoAsync(TurnoCaja turno)
        {
            var pagos = await context.Pagos
                .Where(p => p.IdTurnoCaja == turno.Id && p.EstadoRegistro == EnumEstadoRegistro.activo)
                .Select(p => new { p.IdComanda, p.TipoPago, p.MontoPagado, p.Vuelto })
                .ToListAsync();

            var movimientos = await context.MovimientosCaja
                .Where(m => m.IdTurnoCaja == turno.Id && m.EstadoRegistro == EnumEstadoRegistro.activo)
                .Select(m => new { m.TipoMovimiento, m.Monto })
                .ToListAsync();

            var nombreApertura = await context.Usuarios
                .Where(u => u.Id == turno.IdUsuarioApertura)
                .Select(u => u.Nombre)
                .FirstOrDefaultAsync();

            decimal Cobrado(EnumTipoPago tipo) =>
                pagos.Where(p => p.TipoPago == tipo).Sum(p => p.MontoPagado - p.Vuelto);

            decimal Movido(EnumTipoMovCaja tipo) =>
                movimientos.Where(m => m.TipoMovimiento == tipo).Sum(m => m.Monto);

            return new EstadoCajaDTO
            {
                Abierta = turno.Estado == EnumEstadoTurnoCaja.Abierto,
                Turno = ArmarTurnoDTO(turno, nombreApertura, null),
                MontoInicial = turno.MontoInicial,
                CobrosEfectivo = Cobrado(EnumTipoPago.Efectivo),
                CobrosDebito = Cobrado(EnumTipoPago.Debito),
                CobrosCredito = Cobrado(EnumTipoPago.Credito),
                CobrosTransferencia = Cobrado(EnumTipoPago.Transferencia),
                CobrosQR = Cobrado(EnumTipoPago.QR),
                Ingresos = Movido(EnumTipoMovCaja.Ingreso),
                Egresos = Movido(EnumTipoMovCaja.Egreso),
                Retiros = Movido(EnumTipoMovCaja.Retiro),
                CantidadVentas = pagos.Select(p => p.IdComanda).Distinct().Count()
            };
        }

        private static TurnoCajaDTO ArmarTurnoDTO(TurnoCaja turno, string? nombreApertura, string? nombreCierre) => new()
        {
            Id = turno.Id,
            Estado = turno.Estado,
            FechaApertura = turno.FechaApertura,
            NombreUsuarioApertura = nombreApertura,
            MontoInicial = turno.MontoInicial,
            FechaCierre = turno.FechaCierre,
            NombreUsuarioCierre = nombreCierre,
            MontoEsperado = turno.MontoEsperado,
            MontoContado = turno.MontoContado,
            Diferencia = turno.Diferencia,
            ObservacionesCierre = turno.ObservacionesCierre
        };

        //Fechas locales de Argentina a rango UTC [inicio, fin). Devuelve null si el rango es demasiado largo.
        private static (DateTime Desde, DateTime Hasta, DateTime InicioUtc, DateTime FinUtc)? ArmarRango(DateTime? desde, DateTime? hasta)
        {
            var dia1 = (desde ?? HoraArgentina.HoyLocal).Date;
            var dia2 = (hasta ?? dia1).Date;

            if (dia2 < dia1)
                (dia1, dia2) = (dia2, dia1);

            if ((dia2 - dia1).TotalDays > 366)
                return null;

            return (dia1, dia2, HoraArgentina.InicioDiaUtc(dia1), HoraArgentina.InicioDiaUtc(dia2).AddDays(1));
        }
     
        private async Task<(DateTime Desde, DateTime Hasta, DateTime InicioUtc, DateTime FinUtc)?> ArmarRangoAsync(DateTime? desde, DateTime? hasta, bool todo)
        {
            if (!todo)
                return ArmarRango(desde, hasta);

            var primerPago = await context.Pagos
                .Where(p => p.EstadoRegistro == EnumEstadoRegistro.activo)
                .Select(p => (DateTime?)p.FechaPago)
                .MinAsync();

            var hoy = HoraArgentina.HoyLocal;
            var dia1 = primerPago.HasValue ? HoraArgentina.DesdeUtc(primerPago.Value).Date : hoy;

            return (dia1, hoy, HoraArgentina.InicioDiaUtc(dia1), HoraArgentina.InicioDiaUtc(hoy).AddDays(1));
        }

        private async Task<List<VentaCajaDTO>> ObtenerVentasAsync(DateTime inicioUtc, DateTime finUtc)
        {
            var filas = await context.Pagos
                .Where(p => p.EstadoRegistro == EnumEstadoRegistro.activo && p.FechaPago >= inicioUtc && p.FechaPago < finUtc)
                .Select(p => new
                {
                    p.IdComanda,
                    p.TipoPago,
                    p.MontoPagado,
                    p.Vuelto,
                    p.FechaPago,
                    NombreCajero = p.UsuarioCaja.Nombre,
                    p.Comanda.TipoServicio,
                    FechaApertura = p.Comanda.FechaApertura,
                    NumeroMesa = p.Comanda.Mesa != null ? (int?)p.Comanda.Mesa.NumeroMesa : null
                })
                .ToListAsync();

            var numerosDiarios = await NumeroComandaDiario.ObtenerVariosAsync(
                context,
                filas.Select(f => (f.IdComanda, f.FechaApertura)));

            return filas
                .GroupBy(f => f.IdComanda)
                .Select(g => new VentaCajaDTO
                {
                    IdComanda = g.Key,
                    NumeroDiario = numerosDiarios.GetValueOrDefault(g.Key),
                    TipoServicio = g.First().TipoServicio,
                    NumeroMesa = g.First().NumeroMesa,
                    FechaPago = g.Max(f => f.FechaPago),
                    Vuelto = g.Sum(f => f.Vuelto),
                    Total = g.Sum(f => f.MontoPagado - f.Vuelto),
                    NombreCajero = g.First().NombreCajero,
                    Pagos = g.GroupBy(f => f.TipoPago)
                        .Select(t => new LineaPagoDTO
                        {
                            TipoPago = t.Key,
                            Monto = t.Sum(f => f.MontoPagado - f.Vuelto)
                        })
                        .OrderBy(l => l.TipoPago)
                        .ToList()
                })
                .OrderByDescending(v => v.FechaPago)
                .ToList();
        }

        private static string NombreServicio(EnumTipoServicio tipo) => tipo switch
        {
            EnumTipoServicio.Mesa => "Salón",
            EnumTipoServicio.TakeAway => "Take Away",
            _ => tipo.ToString()
        };
    }
}
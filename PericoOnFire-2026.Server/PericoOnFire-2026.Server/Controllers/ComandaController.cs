using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PericoOnFire_2026.Shared.Utilidades;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.Repositorio.Repositorios;
using PericoOnFire_2026.Server.Servicios;


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

        [HttpGet("Finalizadas")]
        public async Task<ActionResult<HistorialComandasDTO>> Finalizadas(
            [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
            [FromQuery] EnumEstadoComanda? estado, [FromQuery] EnumTipoServicio? tipo,
            [FromQuery] int pagina = 1)
        {
            var hoy = PericoOnFire_2026.Shared.Utilidades.HoraArgentina.HoyLocal;
            var inicioLocal = (desde ?? hoy).Date;
            var finLocal = (hasta ?? hoy).Date;
            if (inicioLocal > finLocal || (finLocal - inicioLocal).TotalDays > 366 || pagina < 1 || pagina > 1000000)
                return BadRequest("Revisá el rango de fechas (máximo un año).");
            if (estado.HasValue && estado != EnumEstadoComanda.Pagada && estado != EnumEstadoComanda.Cancelada)
                return BadRequest("Seleccioná pagadas o canceladas.");
            var inicio = PericoOnFire_2026.Shared.Utilidades.HoraArgentina.InicioDiaUtc(inicioLocal);
            var fin = PericoOnFire_2026.Shared.Utilidades.HoraArgentina.InicioDiaUtc(finLocal.AddDays(1));
            var consulta = context.Comandas.AsNoTracking().Where(c =>
                (c.Estado == EnumEstadoComanda.Pagada || c.Estado == EnumEstadoComanda.Cancelada) &&
                (c.FechaCierre ?? c.FechaApertura) >= inicio && (c.FechaCierre ?? c.FechaApertura) < fin);
            if (estado.HasValue) consulta = consulta.Where(c => c.Estado == estado.Value);
            if (tipo.HasValue) consulta = consulta.Where(c => c.TipoServicio == tipo.Value);
            var total = await consulta.CountAsync();
            var lista = await consulta.OrderByDescending(c => c.FechaCierre ?? c.FechaApertura).ThenByDescending(c => c.Id)
                .Skip((pagina - 1) * 30).Take(30)
                .Include(c => c.Mesa).ThenInclude(m => m!.Sala)
                .Include(c => c.Usuario).Include(c => c.Cliente).Include(c => c.Pagos)
                .Include(c => c.Pedidos).ThenInclude(p => p.DetallesPedido).ThenInclude(d => d.Producto)
                .AsSplitQuery().ToListAsync();
            var numeros = await NumeroComandaDiario.ObtenerVariosAsync(context, lista.Select(c => (c.Id, c.FechaApertura)));
            return Ok(new HistorialComandasDTO
            {
                Total = total,
                Comandas = lista.Select(c => new ComandaHistorialDTO
                {
                    Id = c.Id,
                    NumeroDiario = numeros.GetValueOrDefault(c.Id),
                    NumeroMesa = c.Mesa?.NumeroMesa,
                    Sala = c.Mesa?.Sala?.Nombre,
                    Cliente = c.Cliente?.Nombre,
                    Empleado = c.Usuario?.Nombre,
                    FechaApertura = c.FechaApertura,
                    FechaCierre = c.FechaCierre ?? c.FechaApertura,
                    Estado = c.Estado,
                    TipoServicio = c.TipoServicio,
                    Total = c.Estado == EnumEstadoComanda.Pagada ? c.Total : 0,
                    Pagos = c.Pagos.Select(p => $"{p.TipoPago}: ${p.MontoPagado - p.Vuelto:N2}").ToList(),
                    Items = c.Pedidos.SelectMany(p => p.DetallesPedido.Select(d => new ItemHistorialDTO
                    {
                        Producto = d.Producto.Nombre,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Estado = p.Estado,
                        Observacion = d.Observacion,
                        MotivoCancelacion = p.MotivoCancelacion
                    })).ToList()
                }).ToList()
            });
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

        //Este endpoint obtiene la comanda abierta de una mesa específica
        //y sincronizá el estado de la mesa al abrir una comanda.
        //Si la mesa no tiene una comanda abierta, devuelve un 404 Not Found.
        [HttpGet("Mesa/{idMesa:int}")]
        public async Task<ActionResult<Comanda>> GetByMesa(int idMesa)
        {
            var activas = await context.Comandas.CountAsync(c => c.IdMesa == idMesa &&
                (c.Estado == EnumEstadoComanda.Abierta || c.Estado == EnumEstadoComanda.PendienteCobro));
            if (activas > 1)
                return Conflict("Esta mesa tiene más de una comanda activa. Revisá ambas cuentas antes de cerrar o cobrar; no se borró ningún consumo.");
            var comanda = await context.Comandas
                   .Where(c => c.IdMesa == idMesa && (c.Estado == EnumEstadoComanda.Abierta || c.Estado == EnumEstadoComanda.PendienteCobro))
                   .Select(c => new ComandaDTO
                   {
                       Id = c.Id,
                       IdMesa = c.IdMesa,
                       IdCliente = c.IdCliente,
                       IdUsuario = c.IdUsuario,
                       TipoServicio = c.TipoServicio,
                       Estado = c.Estado,
                       FechaApertura = c.FechaApertura,
                       FechaCierre = c.FechaCierre,
                       Total = c.Total,
                       CantidadComensales = c.CantidadComensales,
                       HoraDeseada = c.HoraDeseada,
                       Observaciones = c.Observaciones
                   })
                   .FirstOrDefaultAsync();

            if (comanda == null)
                return NotFound();

            comanda.NumeroDiario = await NumeroComandaDiario.ObtenerAsync(
                context, comanda.Id, comanda.FechaApertura);

            return Ok(comanda);
        }


        [HttpPost]
        public async Task<ActionResult<int>> Post(ComandaCrearDTO dto)
        {
            return await OperacionAtomica.EjecutarAsync<ActionResult<int>>(context, async () =>
            {
                Mesa? mesa = null;
                if (dto.TipoServicio == EnumTipoServicio.Mesa)
                {
                    if (!dto.IdMesa.HasValue) return BadRequest("Seleccioná una mesa.");
                    mesa = await context.Mesas.FindAsync(dto.IdMesa.Value);
                    if (mesa == null || mesa.EstadoRegistro != EnumEstadoRegistro.activo) return NotFound("La mesa no existe o fue retirada.");
                    if (mesa.Estado != EnumEstadoMesa.Libre ||
                        await context.Comandas.AnyAsync(c => c.IdMesa == mesa.Id &&
                            (c.Estado == EnumEstadoComanda.Abierta || c.Estado == EnumEstadoComanda.PendienteCobro)))
                        return Conflict("La mesa ya tiene una comanda activa. Actualizá la pantalla para agregar productos a esa comanda.");
                }
                else if (dto.IdMesa.HasValue)
                    return BadRequest("Take Away y Delivery no deben ocupar una mesa.");
                var comanda = new Comanda
                {
                    IdMesa = dto.IdMesa,
                    IdCliente = dto.IdCliente,
                    IdUsuario = dto.IdUsuario,
                    TipoServicio = dto.TipoServicio,
                    CantidadComensales = dto.CantidadComensales,
                    Observaciones = dto.Observaciones,
                    Estado = EnumEstadoComanda.Abierta,
                    EstadoRegistro = EnumEstadoRegistro.activo,
                    FechaApertura = DateTime.UtcNow
                };
                context.Comandas.Add(comanda);
                if (mesa != null) mesa.Estado = EnumEstadoMesa.Ocupada;
                await context.SaveChangesAsync();
                return Ok(comanda.Id);
            });
        }

        // Crea la comanda y sus pedidos recién cuando el mozo confirma el primer envío.
        // Pero si algo falla, la mesa continúa libre.
        [HttpPost("Confirmar")]
        public async Task<ActionResult<ComandaConfirmadaDTO>> Confirmar(ConfirmarComandaDTO dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                return BadRequest("Agregá al menos un producto antes de enviar la comanda.");

            if (dto.Items.Any(i => i.Cantidad <= 0 || i.Cantidad > 999 || (i.Observacion?.Length ?? 0) > 300))
                return BadRequest("Las cantidades deben estar entre 1 y 999 y las observaciones no pueden superar los 300 caracteres.");

            var idsProducto = dto.Items.Select(i => i.IdProducto).Distinct().ToList();
            var productos = await context.Productos
                .Where(p => idsProducto.Contains(p.Id) && p.Activo)
                .ToListAsync();

            var faltantes = idsProducto.Except(productos.Select(p => p.Id)).ToList();
            if (faltantes.Any())
                return Conflict($"No existen o están inactivos los productos con id: {string.Join(", ", faltantes)}.");

            ComandaConfirmadaDTO? resultado = null;

            try
            {
                await OperacionAtomica.EjecutarAsync(context, async () =>
                {
                    context.ChangeTracker.Clear();
                    productos = await context.Productos.Where(p => idsProducto.Contains(p.Id) && p.Activo).ToListAsync();
                    if (productos.Count != idsProducto.Count)
                        throw new InvalidOperationException("Hay productos que ya no están disponibles.");

                    var mesa = await context.Mesas.FirstOrDefaultAsync(m => m.Id == dto.IdMesa);
                    if (mesa == null || mesa.EstadoRegistro != EnumEstadoRegistro.activo)
                        throw new InvalidOperationException("La mesa seleccionada no existe o fue retirada.");

                    var tieneComandaAbierta = await context.Comandas.AnyAsync(c =>
                        c.IdMesa == dto.IdMesa &&
                        (c.Estado == EnumEstadoComanda.Abierta || c.Estado == EnumEstadoComanda.PendienteCobro));

                    if (mesa.Estado != EnumEstadoMesa.Libre || tieneComandaAbierta)
                        throw new InvalidOperationException("La mesa ya fue abierta por otro usuario.");

                    var comanda = new Comanda
                    {
                        IdMesa = dto.IdMesa,
                        IdUsuario = dto.IdUsuario,
                        TipoServicio = EnumTipoServicio.Mesa,
                        CantidadComensales = dto.CantidadComensales,
                        Observaciones = dto.Observaciones,
                        Estado = EnumEstadoComanda.Abierta,
                        EstadoRegistro = EnumEstadoRegistro.activo,
                        FechaApertura = DateTime.UtcNow,
                        Total = dto.Items.Sum(i =>
                            productos.First(p => p.Id == i.IdProducto).Precio * i.Cantidad)
                    };

                    context.Comandas.Add(comanda);
                    await context.SaveChangesAsync();

                    var idsPedidos = new List<int>();
                    var grupos = dto.Items.GroupBy(i =>
                        productos.First(p => p.Id == i.IdProducto).SectorDestino);

                    foreach (var grupo in grupos)
                    {
                        var pedido = new Pedido
                        {
                            IdComanda = comanda.Id,
                            SectorDestino = grupo.Key,
                            Estado = EnumEstadoPedido.Pendiente,
                            FechaPedido = DateTime.UtcNow,
                            EstadoRegistro = EnumEstadoRegistro.activo
                        };

                        context.Pedidos.Add(pedido);
                        await context.SaveChangesAsync();

                        foreach (var item in grupo)
                        {
                            var producto = productos.First(p => p.Id == item.IdProducto);
                            context.DetallesPedido.Add(new DetallePedido
                            {
                                IdPedido = pedido.Id,
                                IdProducto = item.IdProducto,
                                Cantidad = item.Cantidad,
                                PrecioUnitario = producto.Precio,
                                Observacion = item.Observacion,
                                EstadoRegistro = EnumEstadoRegistro.activo
                            });
                        }

                        await context.SaveChangesAsync();
                        idsPedidos.Add(pedido.Id);
                    }

                    mesa.Estado = EnumEstadoMesa.Ocupada;
                    await context.SaveChangesAsync();

                    resultado = new ComandaConfirmadaDTO
                    {
                        IdComanda = comanda.Id,
                        NumeroDiario = await NumeroComandaDiario.ObtenerAsync(
                                context, comanda.Id, comanda.FechaApertura),
                        IdsPedidos = idsPedidos
                    };
                    return true;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }

            return Ok(resultado);
        }

        // Para TakeAway/Delivery, aca no hay Mesa que bloquear ni liberar,
        //así que no hace falta la verificación de mesa. En cambio, acá se crea un cliente nuevo
        //con los datos que cargó quien tomó el pedido
        [HttpPost("ConfirmarSinMesa")]
        [Authorize(Roles = "Administracion,Barra")]
        public async Task<ActionResult<ComandaConfirmadaDTO>> ConfirmarSinMesa(ConfirmarPedidoSinMesaDTO dto)
        {
            if (dto.TipoServicio != EnumTipoServicio.TakeAway && dto.TipoServicio != EnumTipoServicio.Delivery)
                return BadRequest("Este endpoint es solo para pedidos TakeAway o Delivery.");

            if (dto.TipoServicio == EnumTipoServicio.Delivery && string.IsNullOrWhiteSpace(dto.Direccion))
                return BadRequest("La dirección es obligatoria para un pedido de Delivery.");

            if (dto.Items == null || !dto.Items.Any())
                return BadRequest("Agregá al menos un producto antes de enviar el pedido.");

            if (dto.Items.Any(i => i.Cantidad <= 0 || i.Cantidad > 999 || (i.Observacion?.Length ?? 0) > 300))
                return BadRequest("Las cantidades deben estar entre 1 y 999 y las observaciones no pueden superar los 300 caracteres.");

            var idsProducto = dto.Items.Select(i => i.IdProducto).Distinct().ToList();
            var productos = await context.Productos
                .Where(p => idsProducto.Contains(p.Id) && p.Activo)
                .ToListAsync();

            var faltantes = idsProducto.Except(productos.Select(p => p.Id)).ToList();
            if (faltantes.Any())
                return Conflict($"No existen o están inactivos los productos con id: {string.Join(", ", faltantes)}.");

            ComandaConfirmadaDTO? resultado = null;
            var estrategia = context.Database.CreateExecutionStrategy();

            try
            {
                await estrategia.ExecuteAsync(async () =>
                {
                    context.ChangeTracker.Clear();
                    await using var transaccion = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                    productos = await context.Productos.Where(p => idsProducto.Contains(p.Id) && p.Activo).ToListAsync();
                    if (productos.Count != idsProducto.Count)
                        throw new InvalidOperationException("Hay productos que ya no están disponibles.");

                    Usuario? cajero = null;
                    TurnoCaja? turno = null;
                    ResultadoCobro? cobro = null;
                    var totalReal = dto.Items.Sum(i => productos.First(p => p.Id == i.IdProducto).Precio * i.Cantidad);
                    if (dto.EfectivoPrevistoDelivery.HasValue)
                    {
                        if (dto.TipoServicio != EnumTipoServicio.Delivery || dto.CobrarAhora)
                            throw new InvalidOperationException("Solo se puede indicar efectivo previsto en un Delivery todavía no cobrado.");
                        if (dto.EfectivoPrevistoDelivery.Value < totalReal || dto.EfectivoPrevistoDelivery.Value > 100000000m)
                            throw new InvalidOperationException("El efectivo previsto debe cubrir el total del Delivery.");
                    }
                    var observacionesEntrega = EfectivoPrevistoDelivery.Guardar(
                        dto.Observaciones, dto.EfectivoPrevistoDelivery);
                    if ((observacionesEntrega?.Length ?? 0) > 300)
                        throw new InvalidOperationException("Las observaciones y la instrucción de efectivo exceden el límite permitido.");

                    if (dto.CobrarAhora)
                    {
                        var identity = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        cajero = await context.Usuarios.FirstOrDefaultAsync(u => u.IdApplicationUser == identity && u.Activo);
                        if (cajero == null)
                            throw new InvalidOperationException("No hay un empleado activo asociado a tu usuario para registrar el cobro.");

                        turno = await CajaActual.ObtenerTurnoAbiertoAsync(context);
                        if (turno == null)
                            throw new InvalidOperationException("La caja está cerrada. Abrí un turno de Caja antes de cobrar.");

                        cobro = CobroCombinado.Evaluar(totalReal, dto.LineasPago);
                        if (!cobro.EsValido)
                            throw new InvalidOperationException(cobro.Error ?? "El cobro no es válido.");
                    }

                    Cliente cliente;
                    if (dto.IdClienteExistente.HasValue)
                    {
                        cliente = await context.Clientes.FirstOrDefaultAsync(c =>
                            c.Id == dto.IdClienteExistente.Value && c.EstadoRegistro == EnumEstadoRegistro.activo)
                            ?? throw new InvalidOperationException("El cliente seleccionado ya no está disponible. Buscalo de nuevo.");
                        // No alterar los datos históricos del cliente al generar una comanda.
                    }
                    else
                    {
                        cliente = new Cliente
                        {
                            Nombre = dto.NombreCliente.Trim(),
                            Direccion = dto.Direccion ?? "",
                            Telefono = dto.Telefono ?? "",
                            EstadoRegistro = EnumEstadoRegistro.activo
                        };
                        context.Clientes.Add(cliente);
                        await context.SaveChangesAsync();
                    }

                    var comanda = new Comanda
                    {
                        IdCliente = cliente.Id,
                        IdUsuario = dto.IdUsuario,
                        TipoServicio = dto.TipoServicio,
                        CantidadComensales = 1,
                        Observaciones = observacionesEntrega,
                        Estado = dto.CobrarAhora ? EnumEstadoComanda.Pagada : EnumEstadoComanda.Abierta,
                        EstadoRegistro = EnumEstadoRegistro.activo,
                        FechaApertura = DateTime.UtcNow,
                        FechaCierre = dto.CobrarAhora ? DateTime.UtcNow : null,
                        HoraDeseada = dto.HoraDeseada,
                        Total = totalReal
                    };

                    context.Comandas.Add(comanda);
                    await context.SaveChangesAsync();

                    var idsPedidos = new List<int>();
                    var grupos = dto.Items.GroupBy(i =>
                        productos.First(p => p.Id == i.IdProducto).SectorDestino);

                    foreach (var grupo in grupos)
                    {
                        var pedido = new Pedido
                        {
                            IdComanda = comanda.Id,
                            SectorDestino = grupo.Key,
                            Estado = EnumEstadoPedido.Pendiente,
                            FechaPedido = DateTime.UtcNow,
                            EstadoRegistro = EnumEstadoRegistro.activo
                        };

                        context.Pedidos.Add(pedido);
                        await context.SaveChangesAsync();

                        foreach (var item in grupo)
                        {
                            var producto = productos.First(p => p.Id == item.IdProducto);
                            context.DetallesPedido.Add(new DetallePedido
                            {
                                IdPedido = pedido.Id,
                                IdProducto = item.IdProducto,
                                Cantidad = item.Cantidad,
                                PrecioUnitario = producto.Precio,
                                Observacion = item.Observacion,
                                EstadoRegistro = EnumEstadoRegistro.activo
                            });
                        }

                        await context.SaveChangesAsync();
                        idsPedidos.Add(pedido.Id);
                    }



                    // Registrar el pago en el mismo turno y transacción que la comanda.
                    // Si falla el pago, se revierte también la creación del pedido.
                    if (dto.CobrarAhora && cobro != null && cajero != null && turno != null)
                    {
                        var ahoraPago = DateTime.UtcNow;
                        context.Pagos.AddRange(cobro.Lineas.Select(l => new Pago
                        {
                            IdComanda = comanda.Id,
                            IdUsuarioCaja = cajero.Id,
                            IdTurnoCaja = turno.Id,
                            TipoPago = l.TipoPago,
                            MontoTotal = totalReal,
                            MontoPagado = l.Monto,
                            Vuelto = l.TipoPago == EnumTipoPago.Efectivo ? cobro.Vuelto : 0,
                            FechaPago = ahoraPago,
                            EstadoRegistro = EnumEstadoRegistro.activo
                        }));
                        await context.SaveChangesAsync();
                    }

                    resultado = new ComandaConfirmadaDTO
                    {
                        IdComanda = comanda.Id,
                        NumeroDiario = await NumeroComandaDiario.ObtenerAsync(
                            context, comanda.Id, comanda.FechaApertura),
                        IdsPedidos = idsPedidos
                    };
                    await transaccion.CommitAsync();
                });

            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
            return Ok(resultado);
        }

        // Permite cargar o cambiar el horario deseado (retiro en Take Away, entrega en
        // Delivery) de una comanda que ya está abierta, sin tener que tocar el resto del pedido.
        [HttpPut("{id:int}/HoraDeseada")]
        public async Task<ActionResult> ActualizarHoraDeseada(int id, ActualizarHoraDeseadaDTO dto)
        {
            var comanda = await context.Comandas.FindAsync(id);
            if (comanda == null) return NotFound();

            if (comanda.Estado != EnumEstadoComanda.Abierta)
                return Conflict("Solo se puede cambiar el horario de una comanda abierta.");
            comanda.HoraDeseada = dto.HoraDeseada;
            await context.SaveChangesAsync();

            return Ok();
        }

        // Permite pasar un pedido de Take Away a Delivery (o al revés) mientras el repartidor
        // todavía no lo tomó. Al pasar a Delivery la dirección es obligatoria; al pasar a
        // Take Away se confirma el nombre de quien lo retira. Los datos del cliente no se
        // modifican: si cambian, se crea un cliente nuevo para no alterar el historial.
        [Authorize(Roles = "Administracion,Barra")]
        [HttpPut("{id:int}/CambiarTipo")]
        public async Task<ActionResult> CambiarTipo(int id, CambiarTipoServicioDTO dto)
        {
            if (dto.NuevoTipo != EnumTipoServicio.TakeAway && dto.NuevoTipo != EnumTipoServicio.Delivery)
                return BadRequest("Solo se puede cambiar entre Take Away y Delivery.");

            return await OperacionAtomica.EjecutarAsync<ActionResult>(context, async () =>
            {
                var comanda = await context.Comandas
                    .Include(c => c.Cliente)
                    .Include(c => c.Pedidos.Where(p => p.Estado != EnumEstadoPedido.Cancelado))
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comanda == null) return NotFound();

                if (comanda.TipoServicio != EnumTipoServicio.TakeAway && comanda.TipoServicio != EnumTipoServicio.Delivery)
                    return Conflict("Solo los pedidos de Take Away o Delivery pueden cambiar de tipo.");

                if (comanda.TipoServicio == dto.NuevoTipo)
                    return Conflict("El pedido ya es de ese tipo.");

                if (comanda.Estado == EnumEstadoComanda.Cancelada)
                    return Conflict("No se puede cambiar el tipo de un pedido cancelado.");

                if (comanda.Pedidos.Any(p => p.IdDelivery.HasValue ||
                                             p.Estado == EnumEstadoPedido.EnCamino ||
                                             p.Estado == EnumEstadoPedido.Entregado))
                    return Conflict("El pedido ya salió a reparto o fue entregado, ya no se puede cambiar el tipo.");

                var direccion = dto.Direccion?.Trim();
                var nombre = string.IsNullOrWhiteSpace(dto.NombreCliente)
                    ? comanda.Cliente?.Nombre
                    : dto.NombreCliente.Trim();

                if (dto.NuevoTipo == EnumTipoServicio.Delivery && string.IsNullOrWhiteSpace(direccion))
                    return BadRequest("La dirección es obligatoria para un pedido de Delivery.");

                if (string.IsNullOrWhiteSpace(nombre))
                    return BadRequest("El nombre del cliente es obligatorio.");

                if (nombre.Length > 100 || (direccion?.Length ?? 0) > 100)
                    return BadRequest("El nombre y la dirección no pueden superar los 100 caracteres.");

                var direccionFinal = dto.NuevoTipo == EnumTipoServicio.Delivery
                    ? direccion!
                    : (comanda.Cliente?.Direccion ?? "");
                var telefono = comanda.Cliente?.Telefono ?? "";

                if (comanda.Cliente == null ||
                    comanda.Cliente.Nombre != nombre ||
                    comanda.Cliente.Direccion != direccionFinal)
                {
                    comanda.Cliente = new Cliente
                    {
                        Nombre = nombre,
                        Direccion = direccionFinal,
                        Telefono = telefono,
                        EstadoRegistro = EnumEstadoRegistro.activo
                    };
                }

                // La previsión de efectivo es solo de Delivery.
                if (dto.NuevoTipo == EnumTipoServicio.TakeAway)
                    comanda.Observaciones = EfectivoPrevistoDelivery.Quitar(comanda.Observaciones);

                comanda.TipoServicio = dto.NuevoTipo;
                await context.SaveChangesAsync();
                return Ok();
            });
        }

        // Reparación puntual de datos de prueba: la mesa 94 fue retirada, pero
        // conservó la comanda 89 y su pedido 117 abiertos. No afecta la mesa 93.
        // Archiva en lugar de borrar físicamente para preservar integridad referencial.
        [Authorize(Roles = "Administracion")]
        [HttpPost("Mantenimiento/ArchivarPruebaMesa12")]
        public async Task<ActionResult> ArchivarPruebaMesa12()
        {
            return await OperacionAtomica.EjecutarAsync<ActionResult>(context, async () =>
            {
                var mesaRetirada = await context.Mesas.FirstOrDefaultAsync(m => m.Id == 94);
                if (mesaRetirada == null || mesaRetirada.NumeroMesa != 12 ||
                    mesaRetirada.EstadoRegistro == EnumEstadoRegistro.activo)
                    return Conflict("La mesa 94 no está retirada. No se realizó ningún cambio.");

                var comanda = await context.Comandas.FirstOrDefaultAsync(c => c.Id == 89);
                if (comanda == null)
                    return NotFound("La comanda de prueba 89 no existe.");
                if (comanda.IdMesa != 94 || comanda.Estado != EnumEstadoComanda.Abierta ||
                    comanda.TipoServicio != EnumTipoServicio.Mesa ||
                    comanda.FechaApertura.Date != new DateTime(2026, 10, 16))
                    return Conflict("La comanda 89 cambió; se canceló la operación para proteger los datos.");

                if (await context.Pagos.AnyAsync(p => p.IdComanda == 89))
                    return Conflict("La comanda registra pagos y no puede archivarse automáticamente.");

                var pedidos = await context.Pedidos.Where(p => p.IdComanda == 89).ToListAsync();
                if (pedidos.Count != 1 || pedidos[0].Id != 117 ||
                    pedidos[0].Estado != EnumEstadoPedido.ListoParaRetirar)
                    return Conflict("Los pedidos de la comanda cambiaron. No se realizó ningún cambio.");

                var ahora = DateTime.UtcNow;
                var pedido = pedidos[0];
                pedido.Estado = EnumEstadoPedido.Cancelado;
                pedido.EstadoRegistro = EnumEstadoRegistro.inactivo;
                pedido.MotivoCancelacion = "Pedido de prueba de mesa duplicada retirada (mantenimiento autorizado).";
                pedido.FechaCancelado = ahora;

                comanda.Estado = EnumEstadoComanda.Cancelada;
                comanda.EstadoRegistro = EnumEstadoRegistro.inactivo;
                comanda.FechaCierre = ahora;
                mesaRetirada.Estado = EnumEstadoMesa.Libre;

                await context.SaveChangesAsync();
                return Ok(new
                {
                    Mensaje = "Comanda de prueba COM-004 y pedido 117 cancelados y archivados.",
                    IdComanda = 89,
                    IdPedido = 117,
                    IdMesaRetirada = 94
                });
            });
        }

        //Este Cancelar sirve en el caso de que se haya abierto una comanda por error,
        //o si el cliente decide no consumir nada y se quiere liberar la mesa.

        [HttpPut("{id:int}/Cancelar")]
        public async Task<ActionResult> Cancelar(int id)
        {
            return await OperacionAtomica.EjecutarAsync<ActionResult>(context, async () =>
            {
                var comanda = await context.Comandas.FindAsync(id);
                if (comanda == null) return NotFound();

                if (comanda.Estado != EnumEstadoComanda.Abierta)
                    return Conflict("Solo se puede cancelar una comanda abierta.");

                //Vacía totalmente, si no tiene ningún pedido real cargado. Los pedidos ya
                //cancelados no cuentan como contenido enotonces si la mesa se abrió y todo lo que se
                //llegó a cargar se canceló después, sigue estando vacía y cualquier usuario
                //tiene que poder liberarla para que otro la pueda usar.
                var tienePedidos = await context.Pedidos.AnyAsync(p =>
                    p.IdComanda == id && p.Estado != EnumEstadoPedido.Cancelado);
                if (tienePedidos)
                    return Conflict("No se puede cancelar una mesa que ya tiene pedidos cargados. Usá 'Cerrar mesa' en su lugar.");

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

        //Y se difernecia de este, Cerrar, porque se usa cuando el cliente ya consumió
        //y se quiere cerrar la comanda para pasar a cobrar. Queda en estado PendienteCobro
        [HttpPut("{id:int}/Cerrar")]
        public async Task<ActionResult> Cerrar(int id)
        {
            return await OperacionAtomica.EjecutarAsync<ActionResult>(context, async () =>
            {
                var comanda = await context.Comandas.FindAsync(id);
                if (comanda == null) return NotFound();

                if (comanda.Estado != EnumEstadoComanda.Abierta)
                    return Conflict("La comanda ya no está abierta.");

                bool hayPedidosSinAvanzar;

                if (comanda.TipoServicio == EnumTipoServicio.Mesa)
                {
                    //Esto es del Mozo, es para que no pueda pasar a caja una comanda
                    //de mesa que todavía tiene pedidos en preparación
                    hayPedidosSinAvanzar = await context.Pedidos.AnyAsync(p =>
                        p.IdComanda == id &&
                        p.Estado != EnumEstadoPedido.Entregado &&
                        p.Estado != EnumEstadoPedido.Cancelado);
                }
                else
                {
                    //Esto es para TakeAway/Delivery en donde pasa a caja apenas cocina/barra lo dejó listo,
                    //sin esperar a que el cliente lo retire o el repartidor lo entregue. Así caja puede cobrar en
                    //cualquier momento del proceso y no solo al final.
                    hayPedidosSinAvanzar = await context.Pedidos.AnyAsync(p =>
                        p.IdComanda == id &&
                        p.Estado != EnumEstadoPedido.ListoParaRetirar &&
                        p.Estado != EnumEstadoPedido.EnCamino &&
                        p.Estado != EnumEstadoPedido.Entregado &&
                        p.Estado != EnumEstadoPedido.Cancelado);
                }

                if (hayPedidosSinAvanzar)
                    return Conflict("Todavía hay pedidos en preparación. Esperá a que cocina/barra los termine antes de pasarlo a caja.");

                comanda.Estado = EnumEstadoComanda.PendienteCobro;

                if (comanda.IdMesa.HasValue)
                {
                    var mesa = await context.Mesas.FindAsync(comanda.IdMesa.Value);
                    if (mesa != null)
                        mesa.Estado = EnumEstadoMesa.PendienteCobro;
                }

                await context.SaveChangesAsync();
                return Ok();

            });
        }
    }
}
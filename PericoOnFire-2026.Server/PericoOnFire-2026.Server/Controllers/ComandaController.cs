using Microsoft.AspNetCore.Mvc;
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
                    Id = c.Id, NumeroDiario = numeros.GetValueOrDefault(c.Id),
                    NumeroMesa = c.Mesa?.NumeroMesa, Sala = c.Mesa?.Sala?.Nombre,
                    Cliente = c.Cliente?.Nombre, Empleado = c.Usuario?.Nombre,
                    FechaApertura = c.FechaApertura, FechaCierre = c.FechaCierre ?? c.FechaApertura,
                    Estado = c.Estado, TipoServicio = c.TipoServicio, Total = c.Estado == EnumEstadoComanda.Pagada ? c.Total : 0,
                    Pagos = c.Pagos.Select(p => $"{p.TipoPago}: ${p.MontoPagado - p.Vuelto:N2}").ToList(),
                    Items = c.Pedidos.SelectMany(p => p.DetallesPedido.Select(d => new ItemHistorialDTO
                    {
                        Producto = d.Producto.Nombre, Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario,
                        Estado = p.Estado, Observacion = d.Observacion, MotivoCancelacion = p.MotivoCancelacion
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
            var comanda = new Comanda
            {
                IdMesa = dto.IdMesa,
                IdCliente = dto.IdCliente,
                IdUsuario = dto.IdUsuario,
                TipoServicio = dto.TipoServicio,
                CantidadComensales = dto.CantidadComensales,
                Observaciones = dto.Observaciones,
                Estado = EnumEstadoComanda.Abierta,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            var id = await repositorio.Insert(comanda);

            if (dto.TipoServicio == EnumTipoServicio.Mesa && dto.IdMesa.HasValue)
            {
                var mesa = await context.Mesas.FindAsync(dto.IdMesa.Value);
                if (mesa != null)
                {
                    mesa.Estado = EnumEstadoMesa.Ocupada;
                    await context.SaveChangesAsync();
                }
            }

            return Ok(id);
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

                    var mesa = await context.Mesas.FirstOrDefaultAsync(m => m.Id == dto.IdMesa);
                    if (mesa == null)
                        throw new InvalidOperationException("La mesa seleccionada no existe.");

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
                    await transaccion.CommitAsync();
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

                var cliente = new Cliente
                {
                    Nombre = dto.NombreCliente,
                    Direccion = dto.Direccion ?? "",
                    Telefono = dto.Telefono ?? "",
                    EstadoRegistro = EnumEstadoRegistro.activo
                };
                context.Clientes.Add(cliente);
                await context.SaveChangesAsync();

                var comanda = new Comanda
                {
                    IdCliente = cliente.Id,
                    IdUsuario = dto.IdUsuario,
                    TipoServicio = dto.TipoServicio,
                    CantidadComensales = 1,
                    Observaciones = dto.Observaciones,
                    Estado = EnumEstadoComanda.Abierta,
                    EstadoRegistro = EnumEstadoRegistro.activo,
                    FechaApertura = DateTime.UtcNow,
                    HoraDeseada = dto.HoraDeseada,
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

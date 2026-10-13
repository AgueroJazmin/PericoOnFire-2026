using Microsoft.AspNetCore.Authorization;
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
            return Ok(await MapearPedidos(pedidos));
        }

        //Este endpoint obtiene los pedidos de un sector específico
        [HttpGet("Sector/{sector}")]
        public async Task<ActionResult<List<PedidoDTO>>> GetBySector(EnumSectorDestino sector)
        {
            var pedidos = await repositorio.SelectBySector(sector);
            return Ok(await MapearPedidos(pedidos));
        }

        //Este endpoint permite cambiar el estado de un pedido específico.
        // Un pedido de TakeAway solo se puede marcar como retirado por el cliente (Entregado)
        // una vez que la cuenta ya se cobró en caja.
        // Delivery se marca Entregado en el momento de la entrega física, sea
        // que ya esté cobrado o se cobre recién ahí.

        [HttpPut("{id:int}/Estado")]
        public async Task<ActionResult> CambiarEstado(int id, CambiarEstadoPedidoDTO dto)
        {
            if (dto.Estado == EnumEstadoPedido.Cancelado && string.IsNullOrWhiteSpace(dto.MotivoCancelacion))
                return BadRequest("Para cancelar un pedido es obligatorio indicar el motivo.");

            if (dto.Estado == EnumEstadoPedido.Entregado)
            {
                var comandaDelPedido = await context.Pedidos
                    .Where(p => p.Id == id)
                    .Select(p => new { p.Comanda.TipoServicio, p.Comanda.Estado })
                    .FirstOrDefaultAsync();

                if (comandaDelPedido != null &&
                    comandaDelPedido.TipoServicio == EnumTipoServicio.TakeAway &&
                    comandaDelPedido.Estado != EnumEstadoComanda.Pagada)
                {
                    return Conflict("Todavía no se registró el pago de este pedido. Primero hay que cobrarlo en caja.");
                }
            }

            try
            {
                var resultado = await repositorio.CambiarEstado(id, dto.Estado, dto.MotivoCancelacion);
                if (!resultado) return NotFound();
                return Ok();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        //Este endpoint borra los pedidos ya entregados de un sector,
        //Es el tacho que aparece al lado de la columna Listos en cocina/barra.
        [HttpDelete("Entregados/Sector/{sector}")]
        public async Task<ActionResult<int>> BorrarEntregadosPorSector(EnumSectorDestino sector)
        {
            var cantidad = await repositorio.EliminarEntregadosPorSector(sector);
            return Ok(cantidad);
        }

        // Se listan las comandas de TakeAway o Delivery que siguen en curso (Abierta o PendienteCobro),
        // con sus pedidos agrupados por sector para saber si ya está todo listo.
        // Cancelado se ignora en todos lados, igual que en el resto del sistema.
        [HttpGet("ParaLlevar/{tipoServicio}")]
        [Authorize(Roles = "Administracion,Barra,Delivery")]
        public async Task<ActionResult<List<PedidoParaLlevarDTO>>> GetParaLlevar(EnumTipoServicio tipoServicio)
        {
            if (tipoServicio != EnumTipoServicio.TakeAway && tipoServicio != EnumTipoServicio.Delivery)
                return BadRequest("Este endpoint es solo para pedidos TakeAway o Delivery.");

            // El rol Delivery solo puede ver pedidos de Delivery, nunca de TakeAway.
            var esSoloDelivery = User.IsInRole("Delivery") &&
                                  !User.IsInRole("Administracion") &&
                                  !User.IsInRole("Barra");

            if (esSoloDelivery && tipoServicio == EnumTipoServicio.TakeAway)
                return Forbid();

            var comandas = await context.Comandas
                .Include(c => c.Cliente)
                .Include(c => c.Pedidos.Where(p => p.Estado != EnumEstadoPedido.Cancelado))
                    .ThenInclude(p => p.DetallesPedido)
                        .ThenInclude(d => d.Producto)
                .Include(c => c.Pedidos.Where(p => p.Estado != EnumEstadoPedido.Cancelado))
                    .ThenInclude(p => p.Delivery)
                .Where(c => c.TipoServicio == tipoServicio &&
                            (c.Estado == EnumEstadoComanda.Abierta ||
                             c.Estado == EnumEstadoComanda.PendienteCobro ||
                             // Aca en esta parte, aun que se haya cobrado, sigue apareciendo hasta
                             // marcarlo como retirado/entregado. Lo hice asi por el momento
                             (c.Estado == EnumEstadoComanda.Pagada &&
                              c.Pedidos.Any(p => p.Estado != EnumEstadoPedido.Entregado &&
                                                  p.Estado != EnumEstadoPedido.Cancelado))))
                .OrderBy(c => c.FechaApertura)
                .ToListAsync();

            var numerosDiarios = await NumeroComandaDiario.ObtenerVariosAsync(
                context,
                comandas.Select(c => (c.Id, c.FechaApertura)));

            var resultado = comandas.Select(c =>
            {
                var pedidosActivos = c.Pedidos.Where(p => p.Estado != EnumEstadoPedido.Cancelado).ToList();

                return new PedidoParaLlevarDTO
                {
                    IdComanda = c.Id,
                    NumeroDiario = numerosDiarios.GetValueOrDefault(c.Id),
                    TipoServicio = c.TipoServicio,
                    EstadoComanda = c.Estado,
                    NombreCliente = c.Cliente?.Nombre ?? "",
                    Telefono = c.Cliente?.Telefono,
                    Direccion = c.Cliente?.Direccion,
                    FechaApertura = c.FechaApertura,
                    HoraDeseada = c.HoraDeseada,
                    Items = pedidosActivos
                        .SelectMany(p => p.DetallesPedido)
                        .Select(d => new ItemCuentaDTO
                        {
                            NombreProducto = d.Producto.Nombre,
                            Cantidad = d.Cantidad,
                            PrecioUnitario = d.PrecioUnitario
                        }).ToList(),
                    Pedidos = pedidosActivos
                        .Select(p => new PedidoSectorEstadoDTO
                        {
                            IdPedido = p.Id,
                            Sector = p.SectorDestino,
                            Estado = p.Estado
                        }).ToList(),
                    IdDelivery = pedidosActivos.Select(p => p.IdDelivery).FirstOrDefault(),
                    NombreDelivery = pedidosActivos.Where(p => p.Delivery != null).Select(p => p.Delivery!.Nombre).FirstOrDefault()
                };
            }).ToList();

            return Ok(resultado);
        }

        // El repartidor toma toda la comanda de una vez, por eso exige que todos los pedidos activos
        //de la comanda estén ListoParaRetirar antes de asignarla. 
        [HttpPut("Delivery/{idComanda:int}/Tomar")]
        [Authorize(Roles = "Delivery,Administracion")]
        public async Task<ActionResult> TomarParaDelivery(int idComanda, AsignarDeliveryDTO dto)
        {
            var comanda = await context.Comandas
                .Include(c => c.Pedidos.Where(p => p.Estado != EnumEstadoPedido.Cancelado))
                .FirstOrDefaultAsync(c => c.Id == idComanda);

            if (comanda == null)
                return NotFound();

            if (comanda.TipoServicio != EnumTipoServicio.Delivery)
                return Conflict("Esta comanda no es un pedido de delivery.");

            var pedidos = comanda.Pedidos.Where(p => p.Estado != EnumEstadoPedido.Cancelado).ToList();

            if (!pedidos.Any())
                return Conflict("Esta comanda no tiene pedidos para repartir.");

            if (pedidos.Any(p => p.IdDelivery.HasValue))
                return Conflict("Este pedido ya fue tomado por otro repartidor.");

            if (pedidos.Any(p => p.Estado != EnumEstadoPedido.ListoParaRetirar))
                return Conflict("Todavía hay productos de este pedido en cocina o barra. Esperá a que estén todos listos.");

            foreach (var pedido in pedidos)
            {
                pedido.IdDelivery = dto.IdUsuarioDelivery;
                pedido.Estado = EnumEstadoPedido.EnCamino;
            }

            await context.SaveChangesAsync();
            return Ok();
        }

        //Cierra el otro extremo del flujo de delivery: de EnCamino a Entregado.
        //Solo lo puede marcar el mismo repartidor que lo tomó antes, y solo si ya estaba EnCamino.
        [HttpPut("Delivery/{idComanda:int}/MarcarEntregado")]
        [Authorize(Roles = "Delivery,Administracion")]
        public async Task<ActionResult> MarcarEntregadoDelivery(int idComanda, AsignarDeliveryDTO dto)
        {
            var comanda = await context.Comandas
                .Include(c => c.Pedidos.Where(p => p.Estado != EnumEstadoPedido.Cancelado))
                .FirstOrDefaultAsync(c => c.Id == idComanda);

            if (comanda == null)
                return NotFound();

            var pedidos = comanda.Pedidos.Where(p => p.Estado != EnumEstadoPedido.Cancelado).ToList();

            if (!pedidos.Any())
                return Conflict("Esta comanda no tiene pedidos para entregar.");

            if (pedidos.Any(p => p.IdDelivery != dto.IdUsuarioDelivery))
                return Conflict("Este pedido no te lo asignaron a vos.");

            if (pedidos.Any(p => p.Estado != EnumEstadoPedido.EnCamino))
                return Conflict("Este pedido todavía no está en camino.");

            foreach (var pedido in pedidos)
            {
                pedido.Estado = EnumEstadoPedido.Entregado;
                pedido.FechaEntregado = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
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


        [HttpPut("Comanda/{idComanda:int}/EditarPendientes")]
        [Authorize(Roles = "Mozo,Administracion")]
        public async Task<ActionResult> EditarPendientes(int idComanda, EditarPedidoPendienteDTO dto)
        {
            if (dto.IdsPedidos == null || dto.IdsPedidos.Count == 0 ||
                dto.Items == null || dto.Items.Count == 0 || dto.DetallesOriginales == null)
                return BadRequest("El pedido debe conservar al menos un producto.");
            if (dto.Items.Any(i => i.Cantidad < 1 || i.Cantidad > 999 || (i.Observacion?.Length ?? 0) > 300))
                return BadRequest("Revisá las cantidades y observaciones.");
            return await OperacionAtomica.EjecutarAsync<ActionResult>(context, async () =>
            {
                var comanda = await context.Comandas.FindAsync(idComanda);
                if (comanda == null || comanda.Estado != EnumEstadoComanda.Abierta ||
                    comanda.TipoServicio != EnumTipoServicio.Mesa)
                    return Conflict("Solo se pueden editar pedidos de una mesa abierta.");
                var ids = dto.IdsPedidos.Distinct().ToList();
                var pedidos = await context.Pedidos.Include(p => p.DetallesPedido)
                    .Where(p => ids.Contains(p.Id) && p.IdComanda == idComanda).ToListAsync();
                if (pedidos.Count != ids.Count || pedidos.Any(p => p.Estado != EnumEstadoPedido.Pendiente))
                    return Conflict("Cocina o Barra ya cambió el estado del pedido. Cerrá el editor y actualizá la mesa.");
                var originales = pedidos.SelectMany(p => p.DetallesPedido).ToList();
                if (originales.Count != dto.DetallesOriginales.Count ||
                    originales.Any(d => !dto.DetallesOriginales.Any(o =>
                        o.Id == d.Id && o.IdProducto == d.IdProducto && o.Cantidad == d.Cantidad &&
                        o.PrecioUnitario == d.PrecioUnitario && o.Observacion == d.Observacion)))
                    return Conflict("Otra persona modificó el pedido. Cerrá el editor y volvé a abrirlo.");
                if (originales.GroupBy(d => d.IdProducto).Any(g => g.Select(d => d.PrecioUnitario).Distinct().Count() > 1))
                    return Conflict("Estos productos tienen precios de distintas rondas. Editalos por separado.");
                var idsProductos = dto.Items.Select(i => i.IdProducto).Distinct().ToList();
                var productos = await context.Productos.Where(p => idsProductos.Contains(p.Id)).ToListAsync();
                if (productos.Count != idsProductos.Count ||
                    productos.Any(p => !p.Activo && !originales.Any(d => d.IdProducto == p.Id)))
                    return Conflict("Hay productos que ya no están disponibles.");
                var grupos = dto.Items.GroupBy(i => productos.First(p => p.Id == i.IdProducto).SectorDestino).ToList();
                // Solo se reemplazan consumos pendientes; los preparados o entregados no se tocan.
                context.DetallesPedido.RemoveRange(originales);
                foreach (var pedido in pedidos)
                {
                    pedido.Estado = EnumEstadoPedido.Cancelado;
                    pedido.MotivoCancelacion = "Reemplazado al editar el pedido pendiente.";
                    pedido.FechaCancelado = DateTime.UtcNow;
                }
                foreach (var grupo in grupos)
                {
                    var destino = pedidos.FirstOrDefault(p => p.SectorDestino == grupo.Key);
                    if (destino == null)
                    {
                        destino = new Pedido { IdComanda = idComanda, SectorDestino = grupo.Key,
                            EstadoRegistro = EnumEstadoRegistro.activo, FechaPedido = DateTime.UtcNow };
                        context.Pedidos.Add(destino);
                    }
                    destino.Estado = EnumEstadoPedido.Pendiente;
                    destino.MotivoCancelacion = null;
                    destino.FechaCancelado = null;
                    // Fuerza la actualización de la fila: un inicio simultáneo de preparación genera conflicto.
                    context.Entry(destino).Property(p => p.Estado).IsModified = true;
                    foreach (var item in grupo)
                    {
                        var producto = productos.First(p => p.Id == item.IdProducto);
                        var precio = originales.FirstOrDefault(d => d.IdProducto == item.IdProducto)?.PrecioUnitario ?? producto.Precio;
                        context.DetallesPedido.Add(new DetallePedido { Pedido = destino,
                            IdProducto = item.IdProducto, Cantidad = item.Cantidad,
                            PrecioUnitario = precio, Observacion = item.Observacion,
                            EstadoRegistro = EnumEstadoRegistro.activo });
                    }
                }
                await context.SaveChangesAsync();
                return Ok();
            });
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
            var pedido = await repositorio.SelectById(id);
            if (pedido == null)
                return NotFound();

            if (pedido.Estado != EnumEstadoPedido.Entregado && pedido.Estado != EnumEstadoPedido.Cancelado)
                return Conflict("Solo se pueden eliminar pedidos entregados o cancelados.");

            var resultado = await repositorio.EliminarConDetalles(id);

            if (!resultado)
                return BadRequest();

            return Ok();
        }

        private async Task<List<PedidoDTO>> MapearPedidos(List<Pedido> pedidos)
        {
            var numerosDiarios = await NumeroComandaDiario.ObtenerVariosAsync(
                context,
                pedidos.Where(p => p.Comanda != null)
                    .Select(p => (p.IdComanda, p.Comanda!.FechaApertura)));

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
                MotivoCancelacion = p.MotivoCancelacion,
                FechaCancelado = p.FechaCancelado,
                NumeroMesa = p.Comanda != null ? p.Comanda.Mesa?.NumeroMesa : null,
                TipoServicio = p.Comanda?.TipoServicio,
                NumeroDiario = numerosDiarios.GetValueOrDefault(p.IdComanda),
                NombreCliente = p.Comanda?.Cliente?.Nombre,
                Direccion = p.Comanda?.Cliente?.Direccion,
                HoraDeseada = p.Comanda?.HoraDeseada,
                DetallesPedido = p.DetallesPedido.Select(d => new DetallePedidoDTO
                {
                    Id = d.Id,
                    IdPedido = d.IdPedido,
                    IdProducto = d.IdProducto,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Observacion = d.Observacion,
                    NombreProducto = d.Producto.Nombre
                }).ToList(),
                DetallesOtroSector = p.Comanda != null
                    ? p.Comanda.Pedidos
                        .Where(hermano => hermano.SectorDestino != p.SectorDestino &&
                                          hermano.Estado != EnumEstadoPedido.Cancelado)
                        .SelectMany(hermano => hermano.DetallesPedido)
                        .Select(d => new DetallePedidoDTO
                        {
                            Id = d.Id,
                            IdPedido = d.IdPedido,
                            IdProducto = d.IdProducto,
                            Cantidad = d.Cantidad,
                            PrecioUnitario = d.PrecioUnitario,
                            Observacion = d.Observacion,
                            NombreProducto = d.Producto.Nombre
                        }).ToList()
                    : new List<DetallePedidoDTO>()
            }).ToList();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Repositorio.Repositorios
{
    public class PedidoRepositorio : Repositorio<Pedido>, IPedidoRepositorio
    {
        private readonly MiDbContext context;

        public PedidoRepositorio(MiDbContext context) : base(context)
        {
            this.context = context;
        }

        public async Task<List<Pedido>> SelectPendientes()
        {
            return await context.Pedidos
                .Include(p => p.Comanda)
                .Where(p => p.Estado == EnumEstadoPedido.Pendiente)
                .ToListAsync();
        }

        public async Task<List<Pedido>> SelectByEstado(EnumEstadoPedido estado)
        {
            return await context.Pedidos
                .Include(p => p.Comanda)
                .Where(p => p.Estado == estado)
                .ToListAsync();
        }

        // Ventana de tiempo antes del horario deseado en la que un pedido empieza a
        // aparecerle a cocina/barra. Si falta más que esto, el pedido existe en la base
        //para que Administración lo vea en el seguimiento pero no estorba en el tablero.
        private const int MinutosAntesDeHoraDeseada = 45;

        public async Task<List<Pedido>> SelectBySector(EnumSectorDestino sector)
        {
            var limite = DateTime.UtcNow.AddMinutes(MinutosAntesDeHoraDeseada);

            return await context.Pedidos
                .Include(p => p.Comanda)
                   .ThenInclude(c => c.Mesa)
                .Include(p => p.Comanda)
                   .ThenInclude(c => c.Cliente)
                .Include(p => p.DetallesPedido)
                   .ThenInclude(d => d.Producto)
                .Include(p => p.Comanda)
                   .ThenInclude(c => c.Pedidos.Where(hermano => hermano.SectorDestino != sector &&
                                                                 hermano.Estado != EnumEstadoPedido.Cancelado))
                       .ThenInclude(hermano => hermano.DetallesPedido)
                           .ThenInclude(d => d.Producto)
                .Where(p => p.EstadoRegistro == EnumEstadoRegistro.activo && p.SectorDestino == sector &&
                            (p.Comanda.HoraDeseada == null || p.Comanda.HoraDeseada <= limite))
                .OrderBy(p => p.FechaPedido)
                .ToListAsync();
        }

        //Este nuevo metodo permite cargar pedidos agrupados por sector,
        //creando un pedido por cada sector y agregando los detalles correspondientes.
        //Es decir, agarra los ítems que cargó el mozo, los agrupa por Producto.SectorDestino
        //y crea un pedido por cada sector con sus DetallesPedido, todo en una sola transacción.
        public async Task<List<int>> CargarPedidoAgrupado(int idComanda, List<ItemPedidoDTO> items)
        {
            if (items == null || !items.Any())
                throw new InvalidOperationException("No hay ítems para cargar.");

            if (items.Any(i => i.Cantidad <= 0 || i.Cantidad > 999 || (i.Observacion?.Length ?? 0) > 300))
                throw new InvalidOperationException("Revisá las cantidades y las observaciones de los productos.");

            var idsProducto = items.Select(i => i.IdProducto).Distinct().ToList();

            var productos = await context.Productos
                .Where(p => idsProducto.Contains(p.Id) && p.Activo)
                .ToListAsync();

            var faltantes = idsProducto.Except(productos.Select(p => p.Id)).ToList();
            if (faltantes.Any())
                throw new InvalidOperationException(
                    $"No existen los productos con id: {string.Join(", ", faltantes)}.");

            var pedidosCreados = new List<int>();

            // La conexión tiene reintentos, así que la transacción también tiene que estar dentro de la estrategia de reintento.
            var estrategia = context.Database.CreateExecutionStrategy();

            await estrategia.ExecuteAsync(async () =>
            {
                pedidosCreados.Clear();
                context.ChangeTracker.Clear();

                using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                try
                {
                    var comanda = await context.Comandas.FirstOrDefaultAsync(c => c.Id == idComanda);
                    if (comanda == null || comanda.Estado != EnumEstadoComanda.Abierta)
                        throw new InvalidOperationException("Solo se pueden agregar productos a una comanda abierta.");
                    productos = await context.Productos.Where(p => idsProducto.Contains(p.Id) && p.Activo).ToListAsync();
                    if (productos.Count != idsProducto.Count)
                        throw new InvalidOperationException("Hay productos que ya no están disponibles.");

                    var gruposPorSector = items.GroupBy(i =>
                        productos.First(p => p.Id == i.IdProducto).SectorDestino);

                    foreach (var grupo in gruposPorSector)
                    {
                        var pedido = new Pedido
                        {
                            IdComanda = idComanda,
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
                        pedidosCreados.Add(pedido.Id);
                    }

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            return pedidosCreados;
        }

        //Este método permite obtener todos los pedidos de una comanda específica,
        //incluyendo sus detalles y los productos asociados.
        public async Task<List<Pedido>> SelectByComanda(int idComanda)
        {
            return await context.Pedidos
                .Include(p => p.Comanda)
                       .ThenInclude(c => c.Mesa)
                .Include(p => p.DetallesPedido)
                       .ThenInclude(d => d.Producto)
                .Where(p => p.IdComanda == idComanda)
                .OrderBy(p => p.FechaPedido)
                .ToListAsync();
        }

        //Este método permite cambiar el estado de un pedido y actualizar las fechas correspondientes
        //Va a ser util para que el sector de cocina o barra pueda marcar un pedido como "En Preparación", "Listo para Retirar" o "Entregado".
        public async Task<bool> CambiarEstado(int id, EnumEstadoPedido nuevoEstado, string? motivoCancelacion)
        {
            var pedido = await context.Pedidos.Include(p => p.Comanda).FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null) return false;

            if (pedido.Estado == nuevoEstado) return true;
            var valido = (pedido.Estado, nuevoEstado) switch
            {
                (EnumEstadoPedido.Pendiente, EnumEstadoPedido.EnPreparacion) => pedido.SectorDestino == EnumSectorDestino.Cocina,
                (EnumEstadoPedido.Pendiente, EnumEstadoPedido.ListoParaRetirar) => pedido.SectorDestino == EnumSectorDestino.Barra,
                (EnumEstadoPedido.EnPreparacion, EnumEstadoPedido.ListoParaRetirar) => true,
                (EnumEstadoPedido.ListoParaRetirar, EnumEstadoPedido.Entregado) => pedido.Comanda.TipoServicio != EnumTipoServicio.Delivery,
                (EnumEstadoPedido.Pendiente, EnumEstadoPedido.Cancelado) => true,
                (EnumEstadoPedido.EnPreparacion, EnumEstadoPedido.Cancelado) => true,
                _ => false
            };
            if (!valido) throw new InvalidOperationException("El estado del pedido cambió. Actualizá la pantalla antes de continuar.");
            if (nuevoEstado == EnumEstadoPedido.Cancelado && string.IsNullOrWhiteSpace(motivoCancelacion))
                throw new InvalidOperationException("Indicá el motivo de cancelación.");
            pedido.Estado = nuevoEstado;
            pedido.MotivoCancelacion = motivoCancelacion;

            switch (nuevoEstado)
            {
                case EnumEstadoPedido.EnPreparacion:
                    pedido.FechaInicioPreparacion = DateTime.UtcNow;
                    break;
                case EnumEstadoPedido.ListoParaRetirar:
                    pedido.FechaListo = DateTime.UtcNow;
                    break;
                case EnumEstadoPedido.Entregado:
                    pedido.FechaEntregado = DateTime.UtcNow;
                    break;
                case EnumEstadoPedido.Cancelado:
                    pedido.MotivoCancelacion = motivoCancelacion;
                    pedido.FechaCancelado = DateTime.UtcNow;
                    break;
            }

            await context.SaveChangesAsync();
            return true;
        }

        //Este método permite vaciar de un solo golpe los pedidos ya entregados de un sector,
        //para el botón "tacho" que aparece al lado de la columna Listos en cocina/barra.
        public async Task<int> EliminarEntregadosPorSector(EnumSectorDestino sector)
        {
            var pedidos = await context.Pedidos
               .Include(p => p.DetallesPedido)
               .Where(p => p.EstadoRegistro == EnumEstadoRegistro.activo && p.SectorDestino == sector && p.Estado == EnumEstadoPedido.Entregado)
               .ToListAsync();

            foreach (var pedido in pedidos)
                pedido.EstadoRegistro = EnumEstadoRegistro.inactivo;
            await context.SaveChangesAsync();
            return pedidos.Count;
        }

        //La llave foranea de DetallesPedido hacia Pedidos es restrict, no lo de cascada, así que
        //Postgres rechaza borrar un Pedido mientras le queden DetallesPedido colgando.
        //Es parecido a lo que hace EliminarEntregadosPorSector, pero para un solo pedido.
        //De esta manera, si un pedido tiene detalles, se borran todos los detalles y luego el pedido.
        public async Task<bool> EliminarConDetalles(int id)
        {
            var pedido = await context.Pedidos
                .Include(p => p.DetallesPedido)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return false;

            if (pedido.Estado != EnumEstadoPedido.Entregado && pedido.Estado != EnumEstadoPedido.Cancelado)
                throw new InvalidOperationException("Solo se pueden archivar pedidos entregados o cancelados.");
            pedido.EstadoRegistro = EnumEstadoRegistro.inactivo;
            await context.SaveChangesAsync();
            return true;
        }
    }
}
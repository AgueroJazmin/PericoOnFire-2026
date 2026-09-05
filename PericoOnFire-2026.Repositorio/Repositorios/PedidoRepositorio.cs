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

        public async Task<List<Pedido>> SelectBySector(EnumSectorDestino sector)
        {
            return await context.Pedidos
                .Include(p => p.Comanda)
                   .ThenInclude(c => c.Mesa)
                .Include(p => p.DetallesPedido)
                   .ThenInclude(d => d.Producto)
                .Where(p => p.SectorDestino == sector)
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

            var idsProducto = items.Select(i => i.IdProducto).Distinct().ToList();

            var productos = await context.Productos
                .Where(p => idsProducto.Contains(p.Id))
                .ToListAsync();

            var faltantes = idsProducto.Except(productos.Select(p => p.Id)).ToList();
            if (faltantes.Any())
                throw new InvalidOperationException(
                    $"No existen los productos con id: {string.Join(", ", faltantes)}.");

            var pedidosCreados = new List<int>();

            // La conexión tiene reintentos automáticos (EnableRetryOnFailure), así que la
            // transacción manual tiene que ir envuelta en la estrategia de ejecución del
            // contexto, o EF la rechaza directamente.
            var estrategia = context.Database.CreateExecutionStrategy();

            await estrategia.ExecuteAsync(async () =>
            {
                pedidosCreados.Clear(); // por si la estrategia reintenta el bloque entero

                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
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
            var pedido = await context.Pedidos.FindAsync(id);
            if (pedido == null) return false;

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
               .Where(p => p.SectorDestino == sector && p.Estado == EnumEstadoPedido.Entregado)
               .ToListAsync();

            foreach (var pedido in pedidos)
            {
                context.DetallesPedido.RemoveRange(pedido.DetallesPedido);
            }

            context.Pedidos.RemoveRange(pedidos);
            await context.SaveChangesAsync();
            return pedidos.Count;
        }

        //La FK de DetallesPedido hacia Pedidos es RESTRICT (no ON DELETE CASCADE), así que
        //Postgres rechaza borrar un Pedido mientras le queden DetallesPedido colgando.
        //Es parecido a lo que hace EliminarEntregadosPorSector, pero para un solo pedido.
        //De esta manera, si un pedido tiene detalles, se borran todos los detalles y luego el pedido.
        public async Task<bool> EliminarConDetalles(int id)
        {
            var pedido = await context.Pedidos
                .Include(p => p.DetallesPedido)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return false;

            context.DetallesPedido.RemoveRange(pedido.DetallesPedido);
            context.Pedidos.Remove(pedido);
            await context.SaveChangesAsync();
            return true;
        }
    }
}

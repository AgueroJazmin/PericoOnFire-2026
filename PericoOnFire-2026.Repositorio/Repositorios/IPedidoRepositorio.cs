using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.Repositorio.Repositorios
{
    public interface IPedidoRepositorio : IRepositorio<Pedido>
    {
        Task<bool> CambiarEstado(int id, EnumEstadoPedido nuevoEstado, string? motivoCancelacion);
        Task<List<int>> CargarPedidoAgrupado(int idComanda, List<ItemPedidoDTO> items);
        Task<bool> EliminarConDetalles(int id);
        Task<int> EliminarEntregadosPorSector(EnumSectorDestino sector);
        Task<List<Pedido>> SelectByComanda(int idComanda);
        Task<List<Pedido>> SelectByEstado(EnumEstadoPedido estado);
        Task<List<Pedido>> SelectBySector(EnumSectorDestino sector);
        Task<List<Pedido>> SelectPendientes();
    }
}
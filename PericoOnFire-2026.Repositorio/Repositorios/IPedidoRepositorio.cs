using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.Repositorio.Repositorios
{
    public interface IPedidoRepositorio : IRepositorio<Pedido>
    {
        Task<List<Pedido>> SelectByEstado(EnumEstadoPedido estado);
        Task<List<Pedido>> SelectBySector(EnumSectorDestino sector);
        Task<List<Pedido>> SelectPendientes();
    }
}
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.Repositorio.Repositorios
{
    public interface IComandaRepositorio : IRepositorio<Comanda>
    {
        Task<List<Comanda>> SelectAbiertas();
        Task<List<Comanda>> SelectByEstado(EnumEstadoComanda estado);
        Task<List<Comanda>> SelectByMesa(int idMesa);
    }
}
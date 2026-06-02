using PericoOnFire_2026.BD.Datos;

namespace PericoOnFire_2026.Repositorio.Repositorios
{
    public interface IRepositorio<E> where E : class, IEntityBase
    {
        Task<bool> Delete(int id);
        Task<bool> Existe(int id);
        Task<int> Insert(E entity);
        Task<List<E>> Select();
        Task<E?> SelectById(int id);
        Task<bool> Update(int id, E entity);
    }
}
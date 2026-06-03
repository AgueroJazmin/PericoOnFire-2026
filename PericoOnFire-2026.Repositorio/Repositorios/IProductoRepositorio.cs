using PericoOnFire_2026.BD.Datos.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Repositorio.Repositorios
{
    public interface IProductoRepositorio : IRepositorio<Producto>
    {
        Task<List<Producto>> SelectActivos();
        Task<List<Producto>> SelectBySubcategoria(int idSubcategoria);
    }
}

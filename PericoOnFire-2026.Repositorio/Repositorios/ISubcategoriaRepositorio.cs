using PericoOnFire_2026.BD.Datos.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Repositorio.Repositorios
{
    public interface ISubcategoriaRepositorio : IRepositorio<Subcategoria>
    {
        Task<List<Subcategoria>> SelectActivas();
        Task<List<Subcategoria>> SelectByCategoria(int idCategoria);
    }
}

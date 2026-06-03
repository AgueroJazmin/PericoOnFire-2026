using PericoOnFire_2026.BD.Datos.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Repositorio.Repositorios
{
    public interface ICategoriaRepositorio : IRepositorio<Categoria>
    {
        Task<List<Categoria>> SelectActivas();
    }
}

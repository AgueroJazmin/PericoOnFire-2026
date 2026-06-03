using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace PericoOnFire_2026.Repositorio.Repositorios
{
    public class CategoriaRepositorio : Repositorio<Categoria>, ICategoriaRepositorio
    {
        private readonly MiDbContext context;

        public CategoriaRepositorio(MiDbContext context) : base(context)
        {
            this.context = context;
        }

        public async Task<List<Categoria>> SelectActivas()
        {
            return await context.Categorias
                .Where(c => c.EstadoRegistro != EnumEstadoRegistro.borrado)
                .ToListAsync();
        }
    }
}

using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace PericoOnFire_2026.Repositorio.Repositorios
{
    public class SubcategoriaRepositorio : Repositorio<Subcategoria>, ISubcategoriaRepositorio
    {
        private readonly MiDbContext context;

        public SubcategoriaRepositorio(MiDbContext context) : base(context)
        {
            this.context = context;
        }

        public async Task<List<Subcategoria>> SelectActivas()
        {
            return await context.Subcategorias
                .Where(x => x.EstadoRegistro != EnumEstadoRegistro.borrado)
                .ToListAsync();
        }

        public async Task<List<Subcategoria>> SelectByCategoria(int idCategoria)
        {
            return await context.Subcategorias
                .Where(x => x.IdCategoria == idCategoria)
                .ToListAsync();
        }
    }
}

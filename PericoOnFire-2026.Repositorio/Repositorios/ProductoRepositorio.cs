using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Repositorio.Repositorios
{
    public class ProductoRepositorio : Repositorio<Producto>, IProductoRepositorio
    {
        private readonly MiDbContext context;

        public ProductoRepositorio(MiDbContext context) : base(context)
        {
            this.context = context;
        }

        public async Task<List<Producto>> SelectActivos()
        {
            return await context.Productos
                .Include(p => p.Subcategoria)
                .Where(p => p.Activo == true)
                .ToListAsync();
        }

        public async Task<List<Producto>> SelectBySubcategoria(int idSubcategoria)
        {
            return await context.Productos
                .Include(p => p.Subcategoria)
                .Where(p => p.IdSubcategoria == idSubcategoria && p.Activo == true)
                .ToListAsync();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Repositorio.Repositorios
{
    public class ComandaRepositorio : Repositorio<Comanda>, IComandaRepositorio
    {
        private readonly MiDbContext context;

        public ComandaRepositorio(MiDbContext context) : base(context)
        {
            this.context = context;
        }

        public async Task<List<Comanda>> SelectAbiertas()
        {
            return await context.Comandas
                .Include(c => c.Mesa)
                .Include(c => c.Cliente)
                .Where(c => c.Estado == EnumEstadoComanda.Abierta)
                .ToListAsync();
        }

        public async Task<List<Comanda>> SelectByEstado(EnumEstadoComanda estado)
        {
            return await context.Comandas
                .Include(c => c.Mesa)
                .Include(c => c.Cliente)
                .Where(c => c.Estado == estado)
                .ToListAsync();
        }

        public async Task<List<Comanda>> SelectByMesa(int idMesa)
        {
            return await context.Comandas
                .Include(c => c.Mesa)
                .Include(c => c.Cliente)
                .Where(c => c.IdMesa == idMesa)
                .ToListAsync();
        }

    }
}

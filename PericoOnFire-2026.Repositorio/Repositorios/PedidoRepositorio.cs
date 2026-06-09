using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Repositorio.Repositorios
{
    public class PedidoRepositorio : Repositorio<Pedido>, IPedidoRepositorio
    {
        private readonly MiDbContext context;

        public PedidoRepositorio(MiDbContext context) : base(context)
        {
            this.context = context;
        }

        public async Task<List<Pedido>> SelectPendientes()
        {
            return await context.Pedidos
                .Include(p => p.Comanda)
                .Where(p => p.Estado == EnumEstadoPedido.Pendiente)
                .ToListAsync();
        }

        public async Task<List<Pedido>> SelectByEstado(EnumEstadoPedido estado)
        {
            return await context.Pedidos
                .Include(p => p.Comanda)
                .Where(p => p.Estado == estado)
                .ToListAsync();
        }

        public async Task<List<Pedido>> SelectBySector(EnumSectorDestino sector)
        {
            return await context.Pedidos
                .Include(p => p.Comanda)
                .Where(p => p.SectorDestino == sector)
                .ToListAsync();
        }
    }
}

using Azure;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using  PericoOnFire_2026.BD.Datos.Entity;
using  PericoOnFire_2026.BD.Datos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;

namespace  PericoOnFire_2026.BD.Datos
{
    public class MiDbContext : IdentityDbContext<ApplicationUser> 
    {

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; } 
        public DbSet<Mesa> Mesas { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Subcategoria> Subcategorias { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Comanda> Comandas { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedido { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<MovimientoCaja> MovimientosCaja { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Aquí puedes configurar tus entidades y relaciones

            var cascadeFKs = modelBuilder.Model
                .G­etEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Casca­de);
            foreach (var fk in cascadeFKs)
            {
                fk.DeleteBehavior = DeleteBehavior.Restr­ict;
            }

            modelBuilder.Entity<Subcategoria>()
        .HasOne(s => s.Categoria)
        .WithMany(c => c.Subcategorias)
        .HasForeignKey(s => s.IdCategoria)
        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Producto>()
                .Property(p => p.Precio)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Comanda>()
                .Property(c => c.Total)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Pago>()
                .Property(p => p.MontoTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Pago>()
                .Property(p => p.MontoPagado)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Pago>()
                .Property(p => p.Vuelto)
                .HasPrecision(18, 2);

            modelBuilder.Entity<DetallePedido>()
                .Property(d => d.PrecioUnitario)
                .HasPrecision(18, 2);

            modelBuilder.Entity<MovimientoCaja>()
                .Property(m => m.Monto)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Producto>()
    .HasOne(p => p.Subcategoria)
    .WithMany()
    .HasForeignKey(p => p.IdSubcategoria)
    .OnDelete(DeleteBehavior.Restrict);
        }

        public MiDbContext(DbContextOptions options) : base(options)
        {
        }

        protected MiDbContext()
        {
        }
    }
}

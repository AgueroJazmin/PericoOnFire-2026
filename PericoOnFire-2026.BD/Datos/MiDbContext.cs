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

            //Esto es porque las claves me daban error y bueno
            //esto es para evitar que se borren en cascada y me den error de clave foranea,
            //lo que hace es que si intento borrar una categoria que tiene subcategorias,
            //no me deje borrarla hasta que borre las subcategorias,
            //lo mismo con los productos y las subcategorias, y con las comandas y los pedidos, etc.

            modelBuilder.Entity<Comanda>()
                .HasOne(c => c.Mesa)
                .WithMany()
                .HasForeignKey(c => c.IdMesa)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comanda>()
                .HasOne(c => c.Cliente)
                .WithMany()
                .HasForeignKey(c => c.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comanda>()
                .HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Comanda)
                .WithMany(c => c.Pedidos)
                .HasForeignKey(p => p.IdComanda)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Delivery)
                .WithMany()
                .HasForeignKey(p => p.IdDelivery)
                .OnDelete(DeleteBehavior.Restrict);

            //Esta es la de datelle pedido, que tiene una relacion con producto,
            //y bueno para evitar el error de clave foranea porque no lo pusimos y se me armaba un lio
            modelBuilder.Entity<DetallePedido>()
                .HasOne(d => d.Pedido)
                .WithMany(p => p.DetallesPedido)
                .HasForeignKey(d => d.IdPedido)
                .OnDelete(DeleteBehavior.Restrict);

            //Aca tambien pongo la especificacion para producto,
            //porque tambien tiene una relacion con producto y me daba error de clave foranea

            modelBuilder.Entity<DetallePedido>()
                .HasOne(d => d.Producto)
                .WithMany(p => p.DetallesPedido)
                .HasForeignKey(d => d.IdProducto)
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

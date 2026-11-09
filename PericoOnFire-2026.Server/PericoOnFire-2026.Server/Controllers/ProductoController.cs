using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Producto")]
    public class ProductosController : ControllerBase
    {
        private readonly IProductoRepositorio repositorio;
        private readonly MiDbContext context;

        public ProductosController(IProductoRepositorio repositorio, MiDbContext context)
        {
            this.repositorio = repositorio;
            this.context = context;
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<ProductoListadoDTO>>> Get()
        {
            var lista = await context.Productos
                        .Include(p => p.Subcategoria)
                        .ThenInclude(s => s.Categoria)
                        .Where(p => p.EstadoRegistro != EnumEstadoRegistro.borrado)
                        .Select(p => new ProductoListadoDTO
                        {
                            Id = p.Id,
                            Nombre = p.Nombre,
                            Precio = p.Precio,
                            IdSubcategoria = p.IdSubcategoria,
                            NombreSubcategoria = p.Subcategoria.NombreSubcategoria,
                            NombreCategoria = p.Subcategoria.Categoria.NombreCategoria,
                            SectorDestino = p.SectorDestino,
                            Activo = p.Activo,
                            // Si es una promo, qué trae adentro (vacío = producto común).
                            Componentes = p.ComponentesPromo
                                .Where(i => i.EstadoRegistro != EnumEstadoRegistro.borrado)
                                .Select(i => new ComponentePromoDTO
                                {
                                    IdProducto = i.IdProductoComponente,
                                    NombreProducto = i.ProductoComponente.Nombre,
                                    Cantidad = i.Cantidad
                                })
                                .ToList()
                        }
                        )

                        .ToListAsync();

            return Ok(lista);
        }
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductoCrearDTO>> GetById(int id)
        {
            var producto = await repositorio.SelectById(id);

            if (producto == null)
                return NotFound($"No existe el producto con id {id}.");

            var dto = new ProductoCrearDTO
            {
                Nombre = producto.Nombre,
                Precio = producto.Precio,
                IdSubcategoria = producto.IdSubcategoria,
                SectorDestino = producto.SectorDestino,
                Activo = producto.Activo
            };

            return Ok(dto);
        }
        [AllowAnonymous]
        [HttpGet("Activos")]
        public async Task<ActionResult<List<Producto>>> GetActivos()
        {
            return await repositorio.SelectActivos();
        }

        [HttpGet("Subcategoria/{idSubcategoria:int}")]
        public async Task<ActionResult<List<Producto>>> GetBySubcategoria(int idSubcategoria)
        {
            return await repositorio.SelectBySubcategoria(idSubcategoria);
        }

        [Authorize(Roles = "Administracion")]
        [HttpPost]
        public async Task<ActionResult<int>> Post(ProductoCrearDTO dto)
        {
            var errorComponentes = await ValidarComponentesAsync(dto, null);
            if (errorComponentes != null)
                return errorComponentes;

            var producto = new Producto
            {
                Nombre = dto.Nombre,
                Precio = dto.Precio,
                IdSubcategoria = dto.IdSubcategoria,
                SectorDestino = dto.SectorDestino,
                Activo = dto.Activo,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            var id = await repositorio.Insert(producto);

            if (dto.Componentes.Any())
                await GuardarComponentesAsync(id, dto.Componentes);

            return Ok(id);
        }

        [Authorize(Roles = "Administracion")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, ProductoCrearDTO dto)
        {
            var producto = await repositorio.SelectById(id);

            if (producto == null)
                return NotFound($"No existe el producto con id {id}.");

            var errorComponentes = await ValidarComponentesAsync(dto, id);
            if (errorComponentes != null)
                return errorComponentes;

            producto.Nombre = dto.Nombre;
            producto.Precio = dto.Precio;
            producto.IdSubcategoria = dto.IdSubcategoria;
            producto.SectorDestino = dto.SectorDestino;
            producto.Activo = dto.Activo;

            var resultado = await repositorio.Update(id, producto);

            if (!resultado)
                return BadRequest("No se pudo actualizar el producto.");

            // Lista vacía = producto común: se le sacan los componentes que pudiera tener.
            await GuardarComponentesAsync(id, dto.Componentes);

            return Ok("Producto actualizado correctamente.");
        }

        [Authorize(Roles = "Administracion")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var producto = await repositorio.SelectById(id);

            if (producto == null)
                return NotFound($"No existe el producto con id {id}.");

            var usadoEnPromos = await context.PromocionItems
                .Where(i => i.IdProductoComponente == id && i.ProductoPromo.EstadoRegistro != EnumEstadoRegistro.borrado)
                .Select(i => i.ProductoPromo.Nombre)
                .Distinct()
                .ToListAsync();
            if (usadoEnPromos.Any())
                return Conflict($"No se puede eliminar: forma parte de la promo {string.Join(", ", usadoEnPromos)}. Quitalo de la promo primero.");

            // Si es una promo, se borra lo que traía adentro.
            var componentesPropios = await context.PromocionItems.Where(i => i.IdProductoPromo == id).ToListAsync();
            if (componentesPropios.Any())
            {
                context.PromocionItems.RemoveRange(componentesPropios);
                await context.SaveChangesAsync();
            }

            try
            {
                var resultado = await repositorio.Delete(id);

                if (!resultado)
                    return BadRequest("No se pudo eliminar el producto.");

                return Ok("Producto eliminado correctamente.");
            }
            catch (InvalidOperationException)
            {
                // El producto ya figura en pedidos viejos: borrarlo de la base rompería ese historial.
                // Se da de baja: desaparece de la configuración, del menú y de los pedidos nuevos,
                // pero los pedidos viejos siguen mostrando su nombre.
                context.ChangeTracker.Clear();
                var baja = await context.Productos.FirstOrDefaultAsync(p => p.Id == id);
                if (baja == null)
                    return NotFound($"No existe el producto con id {id}.");

                baja.Activo = false;
                baja.EstadoRegistro = EnumEstadoRegistro.borrado;
                await context.SaveChangesAsync();

                return Ok("Producto eliminado correctamente.");
            }
        }

        // Esto es la parte de Promos, donde una promo es un producto con precio fijo; PromocionItem dice qué trae adentro.
        // Todas las promos viven en la categoría y subcategoría "Promos" (se crean la primera vez),
        // así aparecen juntas en los menús de salón, Delivery y Take Away.
        [Authorize(Roles = "Administracion")]
        [HttpGet("SubcategoriaPromos")]
        public async Task<ActionResult<int>> GetSubcategoriaPromos()
        {
            var categoria = await context.Categorias.FirstOrDefaultAsync(c =>
                c.NombreCategoria.ToLower() == "promos" && c.EstadoRegistro != EnumEstadoRegistro.borrado);
            if (categoria == null)
            {
                categoria = new Categoria { NombreCategoria = "Promos", EstadoRegistro = EnumEstadoRegistro.activo };
                context.Categorias.Add(categoria);
                await context.SaveChangesAsync();
            }

            var subcategoria = await context.Subcategorias.FirstOrDefaultAsync(s =>
                s.IdCategoria == categoria.Id && s.NombreSubcategoria.ToLower() == "promos" &&
                s.EstadoRegistro != EnumEstadoRegistro.borrado);
            if (subcategoria == null)
            {
                subcategoria = new Subcategoria
                {
                    IdCategoria = categoria.Id,
                    NombreSubcategoria = "Promos",
                    EstadoRegistro = EnumEstadoRegistro.activo
                };
                context.Subcategorias.Add(subcategoria);
                await context.SaveChangesAsync();
            }

            return Ok(subcategoria.Id);
        }

        private async Task<ActionResult?> ValidarComponentesAsync(ProductoCrearDTO dto, int? idPropio)
        {
            if (dto.Componentes == null || dto.Componentes.Count == 0)
                return null;

            if (dto.Componentes.Any(c => c.Cantidad < 1 || c.Cantidad > 99))
                return BadRequest("La cantidad de cada producto de la promo tiene que estar entre 1 y 99.");

            var ids = dto.Componentes.Select(c => c.IdProducto).Distinct().ToList();
            if (idPropio.HasValue && ids.Contains(idPropio.Value))
                return BadRequest("Una promo no puede traerse a sí misma.");

            var productos = await context.Productos
                .Include(p => p.ComponentesPromo)
                .Where(p => ids.Contains(p.Id) && p.EstadoRegistro != EnumEstadoRegistro.borrado)
                .ToListAsync();
            if (productos.Count != ids.Count)
                return BadRequest("Alguno de los productos de la promo no existe.");
            if (productos.Any(p => p.ComponentesPromo.Any()))
                return BadRequest("Una promo no puede traer otra promo.");

            if (idPropio.HasValue &&
                await context.PromocionItems.AnyAsync(i => i.IdProductoComponente == idPropio.Value))
                return BadRequest("Este producto forma parte de otra promo, así que no puede ser una promo.");

            return null;
        }

        // Reemplaza lo que trae la promo por la lista recibida (vacía = deja de ser promo).
        private async Task GuardarComponentesAsync(int idPromo, List<ComponentePromoDTO> componentes)
        {
            var existentes = await context.PromocionItems.Where(i => i.IdProductoPromo == idPromo).ToListAsync();
            context.PromocionItems.RemoveRange(existentes);

            foreach (var grupo in componentes.GroupBy(c => c.IdProducto))
            {
                context.PromocionItems.Add(new PromocionItem
                {
                    IdProductoPromo = idPromo,
                    IdProductoComponente = grupo.Key,
                    Cantidad = Math.Min(99, grupo.Sum(c => c.Cantidad)),
                    EstadoRegistro = EnumEstadoRegistro.activo
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
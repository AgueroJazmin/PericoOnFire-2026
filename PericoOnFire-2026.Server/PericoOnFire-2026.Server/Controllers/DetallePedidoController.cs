using Microsoft.AspNetCore.Mvc;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;
using Microsoft.EntityFrameworkCore;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/DetallePedido")]
    public class DetallePedidoController : ControllerBase
    {
        private readonly MiDbContext context;
        private readonly IRepositorio<DetallePedido> repositorio;

        public DetallePedidoController(IRepositorio<DetallePedido> repositorio, MiDbContext context)
        {
            this.repositorio = repositorio;
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<DetallePedido>>> Get()
        {
            var detalles = await context.DetallesPedido
                .Include(d => d.Producto)
                .Select(d => new 
                {  
                   d.Id,
                   d.Cantidad,
                   Producto = new
                   {
                       d.Producto.Id,
                       d.Producto.Nombre
                   }

                }).ToListAsync();

            return Ok(detalles);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DetallePedido>> Get(int id)
        {
            var detalle = await context.DetallesPedido
                .Include(d => d.Producto)
                .Where(d => d.Id == id)
                .Select(d => new
                {
                    d.Id,
                    d.Cantidad,
                    Producto = new
                    {
                        d.Producto.Id,
                        d.Producto.Nombre
                    }
                }).FirstOrDefaultAsync();

            if (detalle == null)
                return NotFound();

            return Ok(detalle);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(DetallePedidoCrearDTO dto)
        {
            var detalle = new DetallePedido
            {
                IdPedido = dto.IdPedido, 
                IdProducto = dto.IdProducto,
                Cantidad = dto.Cantidad,
                PrecioUnitario = dto.PrecioUnitario,
                Observacion = dto.Observacion,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            var id = await repositorio.Insert(detalle);

            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, DetallePedidoCrearDTO dto)
        {
            var detalle = await repositorio.SelectById(id);

            if (detalle == null)
                return NotFound();

            detalle.IdProducto = dto.IdProducto;
            detalle.Cantidad = dto.Cantidad;
            detalle.PrecioUnitario = dto.PrecioUnitario;
            detalle.Observacion = dto.Observacion;

            var resultado = await repositorio.Update(id, detalle);

            if (!resultado)
                return BadRequest();

            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var resultado = await repositorio.Delete(id);

            if (!resultado)
                return NotFound();

            return Ok();
        }
    }
}

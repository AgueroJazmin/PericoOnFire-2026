using Microsoft.AspNetCore.Mvc;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.Repositorio.Repositorios;


namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Comanda")]
    public class ComandasController : ControllerBase
    {
        private readonly IComandaRepositorio repositorio;
        private readonly MiDbContext context;

        public ComandasController(IComandaRepositorio repositorio,
                                  MiDbContext context)
        {
            this.repositorio = repositorio;
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Comanda>>> Get()
        {
            return await repositorio.Select();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Comanda>> Get(int id)
        {
            var entidad = await repositorio.SelectById(id);

            if (entidad == null)
                return NotFound();

            return entidad;
        }

        [HttpGet("Abiertas")]
        public async Task<ActionResult<List<Comanda>>> GetAbiertas()
        {
            return await repositorio.SelectAbiertas();
        }

        //Este endpoint obtiene la comanda abierta de una mesa específica
        //y sincronizá el estado de la mesa al abrir una comanda.
        //Si la mesa no tiene una comanda abierta, devuelve un 404 Not Found.
        [HttpGet("Mesa/{idMesa:int}")]
        public async Task<ActionResult<Comanda>> GetByMesa(int idMesa)
        {
            var comanda = await context.Comandas
                   .Where(c => c.IdMesa == idMesa && (c.Estado == EnumEstadoComanda.Abierta || c.Estado == EnumEstadoComanda.PendienteCobro))
                   .Select(c => new ComandaDTO
                   {
                       Id = c.Id,
                       IdMesa = c.IdMesa,
                       IdCliente = c.IdCliente,
                       IdUsuario = c.IdUsuario,
                       TipoServicio = c.TipoServicio,
                       Estado = c.Estado,
                       FechaApertura = c.FechaApertura,
                       FechaCierre = c.FechaCierre,
                       Total = c.Total,
                       Observaciones = c.Observaciones
                   }) 
                   .FirstOrDefaultAsync();

            if (comanda == null)
                return NotFound();

            return Ok(comanda);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(ComandaCrearDTO dto)
        {
            var comanda = new Comanda
            {
                IdMesa = dto.IdMesa,
                IdCliente = dto.IdCliente,
                IdUsuario = dto.IdUsuario,
                TipoServicio = dto.TipoServicio,
                Observaciones = dto.Observaciones,
                Estado = EnumEstadoComanda.Abierta,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            var id = await repositorio.Insert(comanda);

            if (dto.TipoServicio == EnumTipoServicio.Mesa && dto.IdMesa.HasValue)
            {
                var mesa = await context.Mesas.FindAsync(dto.IdMesa.Value);
                if (mesa != null)
                {
                    mesa.Estado = EnumEstadoMesa.Ocupada;
                    await context.SaveChangesAsync();
                }
            }

            return Ok(id);
        }

        //Este Cancelar sirve en el caso de que se haya abierto una comanda por error,
        //o si el cliente decide no consumir nada y se quiere liberar la mesa.

        [HttpPut("{id:int}/Cancelar")]
        public async Task<ActionResult> Cancelar(int id)
        {
            var comanda = await context.Comandas.FindAsync(id);
            if (comanda == null) return NotFound();

            if (comanda.Estado != EnumEstadoComanda.Abierta)
                return Conflict("Solo se puede cancelar una comanda abierta.");

            var tienePedidos = await context.Pedidos.AnyAsync(p => p.IdComanda == id);
            if (tienePedidos)
                return Conflict("No se puede cancelar una mesa que ya tiene pedidos cargados. Usá 'Cerrar mesa' en su lugar.");

            comanda.Estado = EnumEstadoComanda.Cancelada;
            comanda.FechaCierre = DateTime.UtcNow;

            if (comanda.IdMesa.HasValue)
            {
                var mesa = await context.Mesas.FindAsync(comanda.IdMesa.Value);
                if (mesa != null)
                    mesa.Estado = EnumEstadoMesa.Libre;
            }

            await context.SaveChangesAsync();
            return Ok();
        }

        //Y se difernecia de este, Cerrar, porque se usa cuando el cliente ya consumió
        //y se quiere cerrar la comanda para pasar a cobrar. Queda en estado PendienteCobro
        [HttpPut("{id:int}/Cerrar")]
        public async Task<ActionResult> Cerrar(int id)
        {
            var comanda = await context.Comandas.FindAsync(id);
            if (comanda == null) return NotFound();

            if (comanda.Estado != EnumEstadoComanda.Abierta)
                return Conflict("La comanda ya no está abierta.");

            comanda.Estado = EnumEstadoComanda.PendienteCobro;

            if (comanda.IdMesa.HasValue)
            {
                var mesa = await context.Mesas.FindAsync(comanda.IdMesa.Value);
                if (mesa != null)
                    mesa.Estado = EnumEstadoMesa.PendienteCobro;
            }

            await context.SaveChangesAsync();
            return Ok();
        }
    }
}

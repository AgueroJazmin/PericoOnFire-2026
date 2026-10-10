using Microsoft.AspNetCore.Authorization;
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
    [Route("api/Mesa")]
    public class MesasController : ControllerBase
    {
        private readonly IRepositorio<Mesa> repositorio;
        private readonly MiDbContext context;

        public MesasController(IRepositorio<Mesa> repositorio, MiDbContext context)
        {
            this.repositorio = repositorio;
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<MesaDTO>>> Get()
        {
            var lista = await context.Mesas
               .Select(m => new MesaDTO
               {
                   Id = m.Id,
                   NumeroMesa = m.NumeroMesa,
                   Estado = m.Estado,
                   IdSala = m.IdSala,
                   NombreSala = m.Sala != null ? m.Sala.Nombre : null,
                   Fila = m.Fila,
                   Columna = m.Columna,
                   Forma = m.Forma,
                   Tamanio = m.Tamanio,
                   TienePedidoListo = context.Pedidos.Any(p =>
                       p.Estado == EnumEstadoPedido.ListoParaRetirar &&
                       p.Comanda.IdMesa == m.Id &&
                       p.Comanda.Estado == EnumEstadoComanda.Abierta),
                   TienePedidoCancelado = context.Pedidos.Any(p =>
                       p.Estado == EnumEstadoPedido.Cancelado &&
                       p.Comanda.IdMesa == m.Id &&
                       p.Comanda.Estado == EnumEstadoComanda.Abierta)
               })
               .ToListAsync();

            return Ok(lista);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Mesa>> Get(int id)
        {
            var mesa = await repositorio.SelectById(id);

            if (mesa == null)
                return NotFound();

            return mesa;
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(MesaDTO dto)
        {
            var mesa = new Mesa
            {
                NumeroMesa = dto.NumeroMesa,
                Estado = dto.Estado,
                IdSala = dto.IdSala,
                Fila = dto.Fila,
                Columna = dto.Columna,
                Forma = dto.Forma,
                Tamanio = dto.Tamanio,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            var id = await repositorio.Insert(mesa);

            return Ok(id);
        }

        // Se usa desde la grilla de Configuración: al clickear una celda vacía de una sala
        // se crea la mesa ahí mismo, con el próximo número disponible (no lo carga el usuario).
        [Authorize(Roles = "Administracion")]
        [HttpPost("EnCelda")]
        public async Task<ActionResult<MesaDTO>> CrearEnCelda(MesaCrearDTO dto)
        {
            var salaExiste = await context.Salas.AnyAsync(s => s.Id == dto.IdSala);
            if (!salaExiste)
                return NotFound("La sala indicada no existe.");

            var ocupada = await context.Mesas.AnyAsync(m =>
                m.IdSala == dto.IdSala && m.Fila == dto.Fila && m.Columna == dto.Columna);
            if (ocupada)
                return Conflict("Esa celda ya tiene una mesa.");

            var siguienteNumero = (await context.Mesas.Select(m => (int?)m.NumeroMesa).MaxAsync()) ?? 0;
            siguienteNumero++;

            var mesa = new Mesa
            {
                NumeroMesa = siguienteNumero,
                Estado = EnumEstadoMesa.Libre,
                IdSala = dto.IdSala,
                Fila = dto.Fila,
                Columna = dto.Columna,
                Forma = dto.Forma,
                Tamanio = dto.Tamanio,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            context.Mesas.Add(mesa);
            await context.SaveChangesAsync();

            return Ok(new MesaDTO
            {
                Id = mesa.Id,
                NumeroMesa = mesa.NumeroMesa,
                Estado = mesa.Estado,
                IdSala = mesa.IdSala,
                Fila = mesa.Fila,
                Columna = mesa.Columna,
                Forma = mesa.Forma,
                Tamanio = mesa.Tamanio
            });
        }

        // Guarda únicamente posiciones; las mesas conservan su identidad y consumo.
        [Authorize(Roles = "Administracion")]
        [HttpPut("Distribucion")]
        public async Task<ActionResult> GuardarDistribucion(DistribucionMesasDTO dto)
        {
            if (dto.Mesas == null || dto.Mesas.Count == 0 ||
                dto.Mesas.Any(p => p.Fila < 0 || p.Columna < 0 || p.Fila > 99 || p.Columna > 99) ||
                dto.Mesas.Select(p => p.Id).Distinct().Count() != dto.Mesas.Count ||
                dto.Mesas.Select(p => (p.Fila, p.Columna)).Distinct().Count() != dto.Mesas.Count)
                return BadRequest("La distribución contiene posiciones inválidas o repetidas.");

            await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var mesasSala = await context.Mesas.Where(m => m.IdSala == dto.IdSala).ToListAsync();
            var posiciones = dto.Mesas.ToDictionary(p => p.Id);
            if (mesasSala.Count != posiciones.Count || mesasSala.Any(m =>
                !posiciones.TryGetValue(m.Id, out var p) ||
                m.Fila != p.FilaOriginal || m.Columna != p.ColumnaOriginal))
                return Conflict("La distribución cambió mientras editabas. Cancelá y recargá para ver la versión actual.");

            foreach (var mesa in mesasSala)
            {
                var posicion = posiciones[mesa.Id];
                mesa.Fila = posicion.Fila;
                mesa.Columna = posicion.Columna;
            }

            try
            {
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok();
            }
            catch (DbUpdateException)
            {
                return Conflict("No se pudo guardar la distribución. Cancelá y recargá antes de volver a intentarlo.");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, MesaDTO dto)
        {
            var mesa = await repositorio.SelectById(id);

            if (mesa == null)
                return NotFound();

            mesa.NumeroMesa = dto.NumeroMesa;
            mesa.Estado = dto.Estado;
            mesa.IdSala = dto.IdSala;
            mesa.Fila = dto.Fila;
            mesa.Columna = dto.Columna;
            mesa.Forma = dto.Forma;
            mesa.Tamanio = dto.Tamanio;

            var resultado = await repositorio.Update(id, mesa);

            if (!resultado)
                return BadRequest();

            return Ok();
        }

        [Authorize(Roles = "Administracion")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var resultado = await repositorio.Delete(id);

                if (!resultado)
                    return NotFound();

                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }
    }
}
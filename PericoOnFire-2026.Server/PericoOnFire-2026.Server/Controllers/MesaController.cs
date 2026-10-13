using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.Server.Servicios;

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
               .Where(m => m.EstadoRegistro == EnumEstadoRegistro.activo)
               .Select(m => new MesaDTO
               {
                   Id = m.Id,
                   NumeroMesa = m.NumeroMesa,
                   Estado = context.Comandas.Any(c => c.IdMesa == m.Id &&
                       c.Estado == EnumEstadoComanda.PendienteCobro)
                       ? EnumEstadoMesa.PendienteCobro
                       : context.Comandas.Any(c => c.IdMesa == m.Id &&
                           c.Estado == EnumEstadoComanda.Abierta)
                           ? EnumEstadoMesa.Ocupada
                           : m.Estado,
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


        [Authorize(Roles = "Administracion")]
        [HttpGet("Diagnostico/{numeroMesa:int}")]
        public async Task<ActionResult> Diagnostico(int numeroMesa)
        {
            var mesas = await context.Mesas.AsNoTracking()
                .Where(m => m.NumeroMesa == numeroMesa)
                .Select(m => new { m.Id, m.NumeroMesa, Sala = m.Sala.Nombre, m.Estado })
                .ToListAsync();
            var ids = mesas.Select(m => m.Id).ToList();
            var comandas = await context.Comandas.AsNoTracking()
                .Where(c => c.IdMesa.HasValue && ids.Contains(c.IdMesa.Value))
                .OrderByDescending(c => c.FechaApertura).Take(30)
                .Select(c => new
                {
                    c.Id, c.IdMesa, c.Estado, c.FechaApertura, c.FechaCierre,
                    Pagos = context.Pagos.Count(p => p.IdComanda == c.Id),
                    Pedidos = c.Pedidos.Select(p => new { p.Id, p.Estado, p.SectorDestino }).ToList()
                }).ToListAsync();
            var numeros = await NumeroComandaDiario.ObtenerVariosAsync(context,
                comandas.Select(c => (c.Id, c.FechaApertura)));
            return Ok(new
            {
                VersionDiagnostico = "mesa-comanda-v1",
                Mesas = mesas.Select(m => new { m.Id, m.NumeroMesa, m.Sala, EstadoGuardado = m.Estado.ToString() }),
                Comandas = comandas.Select(c => new
                {
                    c.Id, c.IdMesa, Numero = $"COM-{numeros.GetValueOrDefault(c.Id):D3}",
                    Estado = c.Estado.ToString(), c.FechaApertura, c.FechaCierre, c.Pagos,
                    Pedidos = c.Pedidos.Select(p => new { p.Id, Estado = p.Estado.ToString(), Sector = p.SectorDestino.ToString() })
                })
            });
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Mesa>> Get(int id)
        {
            var mesa = await repositorio.SelectById(id);

            if (mesa == null)
                return NotFound();

            return mesa;
        }


        private async Task<string?> ValidarMesa(MesaDTO dto, int? excluir = null)
        {
            if (dto.NumeroMesa < 1 || dto.Fila < 0 || dto.Columna < 0 || dto.Fila > 99 || dto.Columna > 99)
                return "Revisá el número y la posición de la mesa.";
            if (!await context.Salas.AnyAsync(s => s.Id == dto.IdSala)) return "La sala no existe.";
            var otras = context.Mesas.Where(m => m.EstadoRegistro == EnumEstadoRegistro.activo &&
                m.IdSala == dto.IdSala && (!excluir.HasValue || m.Id != excluir.Value));
            if (await otras.AnyAsync(m => m.NumeroMesa == dto.NumeroMesa))
                return "Ya existe una mesa con ese número en esta sala.";
            if (await otras.AnyAsync(m => m.Fila == dto.Fila && m.Columna == dto.Columna))
                return "Esa posición ya está ocupada por otra mesa.";
            return null;
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(MesaDTO dto)
        {
            return await OperacionAtomica.EjecutarAsync<ActionResult<int>>(context, async () =>
            {
                var error = await ValidarMesa(dto);
                if (error != null) return Conflict(error);
                var mesa = new Mesa { NumeroMesa = dto.NumeroMesa, Estado = EnumEstadoMesa.Libre,
                    IdSala = dto.IdSala, Fila = dto.Fila, Columna = dto.Columna,
                    Forma = dto.Forma, Tamanio = dto.Tamanio, EstadoRegistro = EnumEstadoRegistro.activo };
                context.Mesas.Add(mesa);
                await context.SaveChangesAsync();
                return Ok(mesa.Id);
            });
        }

        [Authorize(Roles = "Administracion")]
        [HttpPost("EnCelda")]
        public async Task<ActionResult<MesaDTO>> CrearEnCelda(MesaCrearDTO dto)
        {
            return await OperacionAtomica.EjecutarAsync<ActionResult<MesaDTO>>(context, async () =>
            {
                var numero = (await context.Mesas.Where(m => m.EstadoRegistro == EnumEstadoRegistro.activo)
                    .Select(m => (int?)m.NumeroMesa).MaxAsync() ?? 0) + 1;
                var propuesta = new MesaDTO { NumeroMesa = numero, IdSala = dto.IdSala,
                    Fila = dto.Fila, Columna = dto.Columna, Forma = dto.Forma, Tamanio = dto.Tamanio };
                var error = await ValidarMesa(propuesta);
                if (error != null) return Conflict(error);
                var mesa = new Mesa { NumeroMesa = numero, Estado = EnumEstadoMesa.Libre,
                    IdSala = dto.IdSala, Fila = dto.Fila, Columna = dto.Columna,
                    Forma = dto.Forma, Tamanio = dto.Tamanio, EstadoRegistro = EnumEstadoRegistro.activo };
                context.Mesas.Add(mesa);
                await context.SaveChangesAsync();
                propuesta.Id = mesa.Id;
                propuesta.Estado = mesa.Estado;
                return Ok(propuesta);
            });
        }

        [Authorize(Roles = "Administracion")]
        [HttpPost("ResolverDuplicadas")]
        public async Task<ActionResult> ResolverDuplicadas()
        {
            return await OperacionAtomica.EjecutarAsync<ActionResult>(context, async () =>
            {
                var mesas = await context.Mesas.Where(m => m.EstadoRegistro == EnumEstadoRegistro.activo).ToListAsync();
                var activas = await context.Comandas.Where(c => c.IdMesa.HasValue &&
                    (c.Estado == EnumEstadoComanda.Abierta || c.Estado == EnumEstadoComanda.PendienteCobro))
                    .Select(c => c.IdMesa!.Value).Distinct().ToListAsync();
                var grupos = mesas.GroupBy(m => new { m.IdSala, m.NumeroMesa }).Where(g => g.Count() > 1).ToList();
                if (grupos.Any(g => g.Count(m => activas.Contains(m.Id)) > 1))
                    return Conflict("Hay duplicadas con cuentas abiertas en ambas mesas. Revisá sus consumos antes de retirarlas.");
                var retiradas = new List<int>();
                foreach (var grupo in grupos)
                {
                    var conservar = grupo.OrderByDescending(m => activas.Contains(m.Id)).ThenBy(m => m.Id).First();
                    foreach (var duplicada in grupo.Where(m => m.Id != conservar.Id))
                    {
                        duplicada.EstadoRegistro = EnumEstadoRegistro.inactivo;
                        retiradas.Add(duplicada.Id);
                    }
                }
                await context.SaveChangesAsync();
                return Ok(new { Retiradas = retiradas, Mensaje = "Duplicadas retiradas de la grilla. Sus comandas y pagos se conservan." });
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
            try
            {
                return await OperacionAtomica.EjecutarAsync<ActionResult>(context, async () =>
                {
                    var mesasSala = await context.Mesas.Where(m => m.IdSala == dto.IdSala && m.EstadoRegistro == EnumEstadoRegistro.activo).ToListAsync();
                    var posiciones = dto.Mesas.ToDictionary(p => p.Id);
                    if (mesasSala.Count != posiciones.Count || mesasSala.Any(m =>
                        !posiciones.TryGetValue(m.Id, out var p) || m.Fila != p.FilaOriginal || m.Columna != p.ColumnaOriginal))
                        return Conflict("La distribución cambió mientras editabas. Cancelá y recargá para ver la versión actual.");
                    foreach (var mesa in mesasSala)
                    {
                        var posicion = posiciones[mesa.Id];
                        mesa.Fila = posicion.Fila;
                        mesa.Columna = posicion.Columna;
                    }
                    await context.SaveChangesAsync();
                    return Ok();
                });
            }
            catch (DbUpdateException)
            {
                return Conflict("No se pudo guardar la distribución. Cancelá y recargá antes de volver a intentarlo.");
            }
        }


        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, MesaDTO dto)
        {
            return await OperacionAtomica.EjecutarAsync<ActionResult>(context, async () =>
            {
                var mesa = await context.Mesas.FirstOrDefaultAsync(m => m.Id == id && m.EstadoRegistro == EnumEstadoRegistro.activo);
                if (mesa == null) return NotFound();
                var error = await ValidarMesa(dto, id);
                if (error != null) return Conflict(error);
                mesa.NumeroMesa = dto.NumeroMesa;
                mesa.IdSala = dto.IdSala;
                mesa.Fila = dto.Fila; mesa.Columna = dto.Columna;
                mesa.Forma = dto.Forma; mesa.Tamanio = dto.Tamanio;
                // Configuración no cambia el estado operativo de una cuenta.
                await context.SaveChangesAsync();
                return Ok();
            });
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
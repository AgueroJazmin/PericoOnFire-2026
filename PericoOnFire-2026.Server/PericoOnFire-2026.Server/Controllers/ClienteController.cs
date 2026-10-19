using Microsoft.AspNetCore.Mvc;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Cliente")]
    public class ClientesController : ControllerBase
    {
        private readonly IRepositorio<Cliente> repositorio;

        public ClientesController(IRepositorio<Cliente> repositorio)
        {
            this.repositorio = repositorio;
        }

        [HttpGet]
        public async Task<ActionResult<List<Cliente>>> Get()
        {
            return await repositorio.Select();
        }

        // Coincidencias limitadas: no se descarga el padrón completo al navegador.
        [Authorize(Roles = "Administracion,Barra")]
        [HttpGet("Buscar")]
        public async Task<ActionResult<List<ClienteBusquedaDTO>>> Buscar([FromQuery] string? q, [FromServices] MiDbContext context)
        {
            var texto = q?.Trim() ?? "";
            if (texto.Length < 2) return Ok(new List<ClienteBusquedaDTO>());
            if (texto.Length > 70) return BadRequest("La búsqueda es demasiado larga.");

            var resultados = await context.Clientes.AsNoTracking()
                .Where(c => c.EstadoRegistro == EnumEstadoRegistro.activo &&
                    (EF.Functions.ILike(c.Nombre, "%" + texto + "%") ||
                     EF.Functions.ILike(c.Telefono, "%" + texto + "%")))
                .OrderBy(c => c.Nombre)
                .Take(8)
                .Select(c => new ClienteBusquedaDTO
                {
                    Id = c.Id, Nombre = c.Nombre, Telefono = c.Telefono, Direccion = c.Direccion
                })
                .ToListAsync();
            return Ok(resultados);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Cliente>> Get(int id)
        {
            var cliente = await repositorio.SelectById(id);

            if (cliente == null)
                return NotFound();

            return cliente;
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(ClienteCrearDTO dto)
        {
            var cliente = new Cliente
            {
                Nombre = dto.Nombre,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                EstadoRegistro = EnumEstadoRegistro.activo
            };

            var id = await repositorio.Insert(cliente);

            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, ClienteCrearDTO dto)
        {
            var cliente = await repositorio.SelectById(id);

            if (cliente == null)
                return NotFound();

            cliente.Nombre = dto.Nombre;
            cliente.Direccion = dto.Direccion;
            cliente.Telefono = dto.Telefono;

            var resultado = await repositorio.Update(id, cliente);

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

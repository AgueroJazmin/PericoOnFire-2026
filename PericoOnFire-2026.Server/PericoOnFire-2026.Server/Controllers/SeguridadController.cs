using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;
using PericoOnFire_2026.Repositorio.Seguridad;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/seguridad")]
    public class SeguridadController : ControllerBase
    {
        private readonly IServicioSeguridad servicio;

        public SeguridadController(IServicioSeguridad servicio)
        {
            this.servicio = servicio;
        }

        [HttpGet("usuarios")]
        public async Task<ActionResult<List<UsuarioRolDTO>>> ObtenerUsuarios()
        {
            var lista = await servicio.ObtenerUsuarios();
            return Ok(lista);
        }

        [HttpPost("crear-empleado")]
        public async Task<ActionResult> CrearEmpleado(CrearEmpleadoDTO dto)
        {
            var resultado = await servicio.CrearEmpleado(dto);

            if (resultado.Exitoso)
                return Ok();

            // Si falla se devolven los errores traducidos para que la página
            // los muestre al admin. BadRequest con un objeto permite que el cliente lo deserialice.
            return BadRequest(new { errores = resultado.Errores });
        }

        [HttpPost("asignar-rol")]
        public async Task<ActionResult> AsignarRol(AsignarRolDTO dto)
        {
            var resultado = await servicio.AsignarRol(dto);

            switch (resultado)
            {
                case ResultadoOperacionSeguridad.Exitoso:
                    return Ok("Rol asignado correctamente.");
                case ResultadoOperacionSeguridad.NoEncontrado:
                    return NotFound($"No se encontró el usuario con email: {dto.Email}.");
                case ResultadoOperacionSeguridad.Fallido:
                    return BadRequest(new { errores = new List<string> { "No se pudo asignar el rol. Verificá que el rol exista." } });
                default:
                    return BadRequest(new { errores = new List<string> { "Error desconocido." } });
            }
        }

        [HttpPost("remover-rol")]
        public async Task<ActionResult> RemoverRol(AsignarRolDTO dto)
        {
            var resultado = await servicio.RemoverRol(dto);

            switch (resultado)
            {
                case ResultadoOperacionSeguridad.Exitoso:
                    return Ok("Rol removido correctamente.");
                case ResultadoOperacionSeguridad.NoEncontrado:
                    return NotFound($"No se encontró el usuario con email: {dto.Email}.");
                case ResultadoOperacionSeguridad.Fallido:
                    return BadRequest(new { errores = new List<string> { "No se pudo remover el rol." } });
                default:
                    return BadRequest(new { errores = new List<string> { "Error desconocido." } });
            }
        }

        [HttpPost("desactivar-usuario")]
        public async Task<ActionResult> DesactivarUsuario(AsignarRolDTO dto)
        {
            var resultado = await servicio.DesactivarUsuario(dto.Email);
            return resultado switch
            {
                ResultadoOperacionSeguridad.Exitoso => Ok("Usuario desactivado."),
                ResultadoOperacionSeguridad.NoEncontrado => NotFound($"No se encontró el usuario {dto.Email}."),
                _ => BadRequest("No se pudo desactivar el usuario.")
            };
        }

        [HttpPost("activar-usuario")]
        public async Task<ActionResult> ActivarUsuario(AsignarRolDTO dto)
        {
            var resultado = await servicio.ActivarUsuario(dto.Email);
            return resultado switch
            {
                ResultadoOperacionSeguridad.Exitoso => Ok("Usuario activado."),
                ResultadoOperacionSeguridad.NoEncontrado => NotFound($"No se encontró el usuario {dto.Email}."),
                _ => BadRequest("No se pudo activar el usuario.")
            };
        }
    }
}

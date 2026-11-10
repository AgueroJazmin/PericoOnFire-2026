using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;

        public SeguridadController(IServicioSeguridad servicio,
                                   UserManager<ApplicationUser> userManager,
                                   SignInManager<ApplicationUser> signInManager)
        {
            this.servicio = servicio;
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        // Antes ninguno de estos endpoints pedía rol: cualquier usuario logueado (un mozo, por ejemplo)
        // podía listar usuarios, crear empleados o cambiarse el rol. Ahora todo lo de administración
        // exige el rol Administracion. Los endpoints "mi-contrasena" solo exigen estar logueado.
        private const string SoloAdmin = "Administracion";

        [Authorize(Roles = SoloAdmin)]
        [HttpGet("usuarios")]
        public async Task<ActionResult<List<UsuarioRolDTO>>> ObtenerUsuarios()
        {
            var lista = await servicio.ObtenerUsuarios();
            return Ok(lista);
        }

        [Authorize(Roles = SoloAdmin)]
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

        [Authorize(Roles = SoloAdmin)]
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

        [Authorize(Roles = SoloAdmin)]
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

        [Authorize(Roles = SoloAdmin)]
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

        [Authorize(Roles = SoloAdmin)]
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

        // ───────── Contraseñas ─────────

        // El admin consulta la contraseña de un usuario (solo se pide cuando aprieta el ojito).
        [Authorize(Roles = SoloAdmin)]
        [HttpGet("contrasena")]
        public async Task<ActionResult<ContrasenaVisibleDTO>> ObtenerContrasena([FromQuery] string email)
        {
            var contrasena = await servicio.ObtenerContrasenaVisiblePorEmail(email);
            if (contrasena == null)
                return NotFound("Este usuario no tiene la contraseña guardada. Asignale una nueva para poder verla.");

            return Ok(new ContrasenaVisibleDTO { Contrasena = contrasena });
        }

        // El admin define una contraseña nueva para otro usuario.
        [Authorize(Roles = SoloAdmin)]
        [HttpPost("establecer-contrasena")]
        public async Task<ActionResult> EstablecerContrasena(EstablecerContrasenaDTO dto)
        {
            var resultado = await servicio.EstablecerContrasena(dto);
            if (!resultado.Exitoso)
                return BadRequest(new { errores = resultado.Errores });

            // Si el admin se cambió a sí mismo la clave, se renueva su sesión para que no lo desloguee.
            var actual = await userManager.GetUserAsync(User);
            if (actual != null && string.Equals(actual.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
                await signInManager.RefreshSignInAsync(actual);

            return Ok();
        }

        // Cualquier usuario logueado ve su propia contraseña.
        [Authorize]
        [HttpGet("mi-contrasena")]
        public async Task<ActionResult<ContrasenaVisibleDTO>> ObtenerMiContrasena()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(id)) return Unauthorized();

            var contrasena = await servicio.ObtenerContrasenaVisiblePorId(id);
            if (contrasena == null)
                return NotFound("Tu contraseña todavía no está guardada. Cambiala una vez para poder verla acá.");

            return Ok(new ContrasenaVisibleDTO { Contrasena = contrasena });
        }

        // Cualquier usuario logueado cambia su propia contraseña (confirmando la actual).
        [Authorize]
        [HttpPost("cambiar-mi-contrasena")]
        public async Task<ActionResult> CambiarMiContrasena(CambiarMiContrasenaDTO dto)
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(id)) return Unauthorized();

            var resultado = await servicio.CambiarContrasenaPropia(id, dto);
            if (!resultado.Exitoso)
                return BadRequest(new { errores = resultado.Errores });

            // Cambiar la clave invalida la sesión anterior; se renueva para que el usuario siga logueado.
            var usuario = await userManager.FindByIdAsync(id);
            if (usuario != null)
                await signInManager.RefreshSignInAsync(usuario);

            return Ok();
        }
    }
}
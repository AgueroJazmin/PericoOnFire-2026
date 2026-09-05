using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/ConfiguracionSalon")]
    public class ConfiguracionSalonController : ControllerBase
    {
        private const int TamanioMaximo = 5 * 1024 * 1024;
        private static readonly string[] TiposPermitidos =
            { "image/jpeg", "image/png", "image/webp" };

        private readonly MiDbContext context;

        public ConfiguracionSalonController(MiDbContext context)
        {
            this.context = context;
        }

        [AllowAnonymous]
        [HttpGet("Foto")]
        public async Task<ActionResult> ObtenerFoto()
        {
            var configuracion = await context.ConfiguracionesSalon
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            if (configuracion == null || configuracion.Foto.Length == 0)
                return NotFound();

            return File(configuracion.Foto, configuracion.MimeType);
        }

        [Authorize(Roles = "Administracion")]
        [HttpPut("Foto")]
        public async Task<ActionResult> CambiarFoto(FotoSalonDTO dto)
        {
            if (!TiposPermitidos.Contains(dto.MimeType.ToLowerInvariant()))
                return BadRequest("La imagen debe ser JPG, PNG o WebP.");

            byte[] contenido;
            try
            {
                contenido = Convert.FromBase64String(dto.ContenidoBase64);
            }
            catch (FormatException)
            {
                return BadRequest("El contenido de la imagen no es válido.");
            }

            if (contenido.Length == 0 || contenido.Length > TamanioMaximo)
                return BadRequest("La imagen debe pesar menos de 5 MB.");

            var configuracion = await context.ConfiguracionesSalon
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            if (configuracion == null)
            {
                configuracion = new ConfiguracionSalon
                {
                    EstadoRegistro = EnumEstadoRegistro.activo
                };
                context.ConfiguracionesSalon.Add(configuracion);
            }

            configuracion.Foto = contenido;
            configuracion.MimeType = dto.MimeType.ToLowerInvariant();
            configuracion.NombreArchivo = string.IsNullOrWhiteSpace(dto.NombreArchivo)
                ? "foto-salon"
                : dto.NombreArchivo[..Math.Min(dto.NombreArchivo.Length, 200)];

            await context.SaveChangesAsync();
            return Ok();
        }
    }
}

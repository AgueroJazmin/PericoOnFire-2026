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
        public async Task<ActionResult> ObtenerFoto([FromQuery] int? id)
        {
            var consulta = context.ConfiguracionesSalon.AsNoTracking();
            if (id.HasValue) consulta = consulta.Where(c => c.Id == id.Value);
            var configuracion = await consulta.OrderBy(c => c.Id).FirstOrDefaultAsync();

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

        [HttpGet("Fotos")]
        public async Task<ActionResult<List<FotoPanelDTO>>> Fotos()
        {
            return Ok(await context.ConfiguracionesSalon.AsNoTracking()
                .Where(c => c.Foto.Length > 0).OrderBy(c => c.Id)
                .Select(c => new FotoPanelDTO { Id = c.Id, Nombre = c.NombreArchivo }).ToListAsync());
        }

        [Authorize(Roles = "Administracion")]
        [HttpPost("Fotos")]
        public async Task<ActionResult> AgregarFoto(FotoSalonDTO dto)
        {
            if (!TiposPermitidos.Contains(dto.MimeType.ToLowerInvariant()))
                return BadRequest("La imagen debe ser JPG, PNG o WebP.");
            byte[] contenido;
            try { contenido = Convert.FromBase64String(dto.ContenidoBase64); }
            catch (FormatException) { return BadRequest("El contenido de la imagen no es válido."); }
            if (contenido.Length == 0 || contenido.Length > TamanioMaximo)
                return BadRequest("Cada foto debe pesar como máximo 5 MB.");
            return await OperacionAtomica.EjecutarAsync<ActionResult>(context, async () =>
            {
                if (await context.ConfiguracionesSalon.CountAsync() >= 8)
                    return Conflict("Podés tener hasta 8 fotos. Quitá una antes de agregar otra.");
                context.ConfiguracionesSalon.Add(new ConfiguracionSalon
                {
                    Foto = contenido, MimeType = dto.MimeType.ToLowerInvariant(),
                    NombreArchivo = string.IsNullOrWhiteSpace(dto.NombreArchivo) ? "foto-salon" :
                        dto.NombreArchivo[..Math.Min(dto.NombreArchivo.Length, 200)],
                    EstadoRegistro = EnumEstadoRegistro.activo
                });
                await context.SaveChangesAsync();
                return Ok();
            });
        }

        [Authorize(Roles = "Administracion")]
        [HttpDelete("Fotos/{id:int}")]
        public async Task<ActionResult> QuitarFoto(int id)
        {
            var foto = await context.ConfiguracionesSalon.FindAsync(id);
            if (foto == null) return NotFound();
            context.ConfiguracionesSalon.Remove(foto);
            await context.SaveChangesAsync();
            return Ok();
        }
    }
}

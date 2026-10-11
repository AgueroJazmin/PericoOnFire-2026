using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.Utilidades;
using System.Globalization;
using System.Text.Json;
using System.Security.Claims;
namespace PericoOnFire_2026.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/PanelBar")]
public class PanelBarController : ControllerBase
{
    private const string TipoClaim = "perico:cumpleanos";
    private readonly MiDbContext context;
    public PanelBarController(MiDbContext context) { this.context = context; }

    [HttpGet("Cumpleanos")]
    public async Task<ActionResult<List<CumpleanosEmpleadoDTO>>> Cumpleanos()
    {
        var empleados = await ObtenerEmpleados();
        empleados.AddRange((await ObtenerPersonas()).Select(p => new CumpleanosEmpleadoDTO { Id = $"persona-{p.Id}", Nombre = p.Nombre, Dia = p.Dia, Mes = p.Mes }));
        var hoy = HoraArgentina.HoyLocal;
        foreach (var e in empleados.Where(e => e.Dia.HasValue && e.Mes.HasValue))
        {
            var dia = Math.Min(e.Dia!.Value, DateTime.DaysInMonth(hoy.Year, e.Mes!.Value));
            var fecha = new DateTime(hoy.Year, e.Mes.Value, dia);
            if (fecha < hoy)
                fecha = new DateTime(hoy.Year + 1, e.Mes.Value, Math.Min(e.Dia.Value, DateTime.DaysInMonth(hoy.Year + 1, e.Mes.Value)));
            e.Proximo = fecha;
        }
        return Ok(empleados.Where(e => e.Proximo.HasValue).OrderBy(e => e.Proximo).ThenBy(e => e.Nombre).ToList());
    }

    [Authorize(Roles = "Administracion")]
    [HttpGet("Empleados")]
    public async Task<ActionResult<List<CumpleanosEmpleadoDTO>>> Empleados() => Ok(await ObtenerEmpleados());

    private async Task<List<CumpleanosEmpleadoDTO>> ObtenerEmpleados()
    {
        var ahora = DateTimeOffset.UtcNow;
        var usuarios = await context.Users.AsNoTracking()
            .Where(u => u.LockoutEnd == null || u.LockoutEnd < ahora)
            .Select(u => new { u.Id, u.UserName }).ToListAsync();
        var ids = usuarios.Select(u => u.Id).ToList();
        var nombres = await context.UserClaims.AsNoTracking()
            .Where(c => ids.Contains(c.UserId) && (c.ClaimType == "nombre" || c.ClaimType == TipoClaim))
            .ToListAsync();
        var negocio = await context.Usuarios.AsNoTracking().Where(u => ids.Contains(u.IdApplicationUser)).ToListAsync();
        var lista = new List<CumpleanosEmpleadoDTO>();
        foreach (var usuario in usuarios)
        {
            var perfil = negocio.FirstOrDefault(u => u.IdApplicationUser == usuario.Id);
            if (perfil != null && !perfil.Activo) continue;
            var e = new CumpleanosEmpleadoDTO { Id = usuario.Id,
                Nombre = perfil?.Nombre ?? nombres.FirstOrDefault(c => c.UserId == usuario.Id && c.ClaimType == "nombre")?.ClaimValue ?? usuario.UserName ?? "Empleado" };
            var valor = nombres.FirstOrDefault(c => c.UserId == usuario.Id && c.ClaimType == TipoClaim)?.ClaimValue;
            if (DateTime.TryParseExact("2000-" + valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            { e.Dia = fecha.Day; e.Mes = fecha.Month; }
            lista.Add(e);
        }
        return lista.OrderBy(e => e.Nombre).ToList();
    }

    [Authorize(Roles = "Administracion")]
    [HttpPut("Empleados/{id}/Cumpleanos")]
    public async Task<ActionResult> Guardar(string id, GuardarCumpleanosDTO dto)
    {
        if (dto.Dia.HasValue != dto.Mes.HasValue)
            return BadRequest("Completá día y mes, o dejá ambos vacíos.");
        if (dto.Dia.HasValue && (dto.Mes < 1 || dto.Mes > 12 || dto.Dia < 1 ||
            dto.Dia > DateTime.DaysInMonth(2000, dto.Mes!.Value)))
            return BadRequest("La fecha del cumpleaños no es válida.");
        return await OperacionAtomica.EjecutarAsync<ActionResult>(context, async () =>
        {
            if (!await context.Users.AnyAsync(u => u.Id == id)) return NotFound("El empleado ya no existe.");
            var anteriores = await context.UserClaims.Where(c => c.UserId == id && c.ClaimType == TipoClaim).ToListAsync();
            context.UserClaims.RemoveRange(anteriores);
            if (dto.Dia.HasValue)
                context.UserClaims.Add(new IdentityUserClaim<string> { UserId = id, ClaimType = TipoClaim,
                    ClaimValue = $"{dto.Mes:00}-{dto.Dia:00}" });
            await context.SaveChangesAsync();
            return Ok();
        });
    }

    private const string ClaimPersona = "perico:cumpleanos-persona";
    private async Task<List<CumpleanosPersonaDTO>> ObtenerPersonas()
    {
        var registros = await context.UserClaims.AsNoTracking().Where(c => c.ClaimType == ClaimPersona).ToListAsync();
        var lista = new List<CumpleanosPersonaDTO>();
        foreach (var registro in registros)
        {
            var persona = JsonSerializer.Deserialize<CumpleanosPersonaDTO>(registro.ClaimValue ?? "{}");
            if (persona == null) continue;
            persona.Id = registro.Id;
            lista.Add(persona);
        }
        return lista.OrderBy(p => p.Nombre).ToList();
    }

    [Authorize(Roles = "Administracion")]
    [HttpGet("Personas")]
    public async Task<ActionResult<List<CumpleanosPersonaDTO>>> Personas() => Ok(await ObtenerPersonas());

    [Authorize(Roles = "Administracion")]
    [HttpPost("Personas")]
    public async Task<ActionResult> GuardarPersona(CumpleanosPersonaDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || dto.Nombre.Trim().Length > 100)
            return BadRequest("Ingresá un nombre de hasta 100 caracteres.");
        if (!new[] { "Empleado", "Cliente", "Proveedor", "Otro" }.Contains(dto.Tipo))
            return BadRequest("Elegí un tipo de persona válido.");
        if (dto.Mes < 1 || dto.Mes > 12 || dto.Dia < 1 || dto.Dia > DateTime.DaysInMonth(2000, dto.Mes))
            return BadRequest("Ingresá un día y mes válidos.");
        return await OperacionAtomica.EjecutarAsync<ActionResult>(context, async () =>
        {
            IdentityUserClaim<string>? registro;
            if (dto.Id == 0)
            {
                var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (usuarioId == null || !await context.Users.AnyAsync(u => u.Id == usuarioId)) return Unauthorized();
                registro = new IdentityUserClaim<string> { UserId = usuarioId, ClaimType = ClaimPersona };
                context.UserClaims.Add(registro);
            }
            else
            {
                registro = await context.UserClaims.FirstOrDefaultAsync(c => c.Id == dto.Id && c.ClaimType == ClaimPersona);
                if (registro == null) return NotFound();
            }
            dto.Nombre = dto.Nombre.Trim();
            registro.ClaimValue = JsonSerializer.Serialize(dto);
            await context.SaveChangesAsync();
            return Ok();
        });
    }
}

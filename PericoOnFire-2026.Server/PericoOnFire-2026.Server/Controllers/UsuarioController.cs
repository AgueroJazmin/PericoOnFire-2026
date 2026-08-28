using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Usuario")]
    
    public class UsuariosController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly MiDbContext context;

        public UsuariosController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            MiDbContext context)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.context = context;
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<UsuarioListadoDTO>>> Get()
        {
            var usuarios = await userManager.Users.ToListAsync();

            var lista = new List<UsuarioListadoDTO>();

            foreach (var u in usuarios)
            {
                var roles = await userManager.GetRolesAsync(u);

                lista.Add(new UsuarioListadoDTO
                {
                    Id = u.Id,
                    Email = u.Email ?? "",
                    UserName = u.UserName ?? "",
                    Rol = roles.FirstOrDefault() ?? "",
                    EmailConfirmed = u.EmailConfirmed
                });
            }

            return Ok(lista);
        }

        //Este endpoint obtiene el usuario actual basado en el token JWT,
        //lo busca en la base de datos y devuelve su Id.
        //Esto es útil para que el cliente sepa qué usuario de negocio está logueado.     
        [HttpGet("Actual")]
        public async Task<ActionResult<int>> ObtenerUsuarioActual()
        {
            var idApplicationUser = userManager.GetUserId(User);
            if (idApplicationUser == null)
                return Unauthorized();

            var usuario = await context.Usuarios
                .FirstOrDefaultAsync(u => u.IdApplicationUser == idApplicationUser);

            if (usuario == null)
                return NotFound("No se encontró un usuario de negocio asociado a esta cuenta.");

            return Ok(usuario.Id);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Post(UsuarioCrearDTO dto)
        {
            if (!await roleManager.RoleExistsAsync(dto.Rol))
                return BadRequest("El rol seleccionado no existe.");

            var usuario = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true
            };

            var resultado = await userManager.CreateAsync(usuario, dto.Password);

            if (!resultado.Succeeded)
                return BadRequest(string.Join(" - ", resultado.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(usuario, dto.Rol);

            // usa dto.Nombre en lugar de dto.Email
            var nombreClaim = !string.IsNullOrWhiteSpace(dto.Nombre) ? dto.Nombre : dto.Email;
            await userManager.AddClaimAsync(usuario,
                new System.Security.Claims.Claim("nombre", nombreClaim));

            // NUEVO: completa el alta creando también la fila de negocio en Usuarios,
            // así no hace falta vincularla a mano en Neon cada vez.
            context.Usuarios.Add(new Usuario
            {
                IdApplicationUser = usuario.Id,
                Nombre = nombreClaim,
                Activo = true,
                EstadoRegistro = EnumEstadoRegistro.activo
            });
            await context.SaveChangesAsync();


            return Ok();
        }

        [AllowAnonymous]
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(string id, UsuarioEditarDTO dto)
        {
            var usuario = await userManager.FindByIdAsync(id);

            if (usuario == null)
                return NotFound("No se encontró el usuario.");

            usuario.Email = dto.Email;
            usuario.UserName = dto.Email;
            usuario.EmailConfirmed = dto.EmailConfirmed;

            var resultado = await userManager.UpdateAsync(usuario);

            if (!resultado.Succeeded)
                return BadRequest(string.Join(" - ", resultado.Errors.Select(e => e.Description)));

            var rolesActuales = await userManager.GetRolesAsync(usuario);
            await userManager.RemoveFromRolesAsync(usuario, rolesActuales);

            if (!string.IsNullOrWhiteSpace(dto.Rol))
                await userManager.AddToRoleAsync(usuario, dto.Rol);

            return Ok();
        }
    }
}

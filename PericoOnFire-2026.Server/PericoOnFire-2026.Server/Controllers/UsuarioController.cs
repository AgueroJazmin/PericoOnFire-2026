using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.DTOs;

namespace PericoOnFire_2026.Server.Controllers
{
    [ApiController]
    [Route("api/Usuario")]
    
    public class UsuariosController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public UsuariosController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
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

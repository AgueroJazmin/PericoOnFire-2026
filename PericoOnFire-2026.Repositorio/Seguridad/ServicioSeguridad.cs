using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Repositorio.Seguridad
{
    public class ServicioSeguridad : IServicioSeguridad
    {
        private readonly MiDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ICifradoContrasenas cifrado;

        // La copia cifrada de la contraseña se guarda en la tabla estándar de Identity AspNetUserTokens
        // (no requiere migración y, a diferencia de un claim, nunca viaja dentro de la cookie de sesión).
        private const string ProveedorToken = "PericoOnFire";
        private const string NombreToken = "ContrasenaVisible";

        public ServicioSeguridad(MiDbContext context,
                                 UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
                                 ICifradoContrasenas cifrado)
        {
            this.context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.cifrado = cifrado;
        }

        public async Task<List<UsuarioRolDTO>> ObtenerUsuarios()
        {
            var usuarios = await context.Users
                .OrderBy(u => u.UserName)
                .AsNoTracking()
                .ToListAsync();

            // Nombre completo (tabla Usuarios) y quiénes tienen contraseña visible, en una sola consulta cada uno
            var nombres = (await context.Usuarios.AsNoTracking().ToListAsync())
                .GroupBy(x => x.IdApplicationUser)
                .ToDictionary(g => g.Key, g => g.First().Nombre);

            var conContrasenaVisible = new HashSet<string>(
                await context.UserTokens
                    .Where(t => t.LoginProvider == ProveedorToken && t.Name == NombreToken)
                    .Select(t => t.UserId)
                    .ToListAsync());

            var resultado = new List<UsuarioRolDTO>();

            foreach (var u in usuarios)
            {
                // GetRolesAsync consulta la tabla AspNetUserRoles de Identity
                var roles = await userManager.GetRolesAsync(u);

                if (!nombres.TryGetValue(u.Id, out var nombre))
                {
                    var claims = await userManager.GetClaimsAsync(u);
                    nombre = claims.FirstOrDefault(c => c.Type == "nombre")?.Value ?? "";
                }

                resultado.Add(new UsuarioRolDTO
                {
                    Id = u.Id,
                    Email = u.Email!,
                    Nombre = nombre,
                    TieneContrasenaVisible = cifrado.Habilitado && conContrasenaVisible.Contains(u.Id),
                    Roles = roles.ToList(),
                    Activo = u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.UtcNow
                });
            }

            return resultado;
        }

        public async Task<ResultadoCrearEmpleado> CrearEmpleado(CrearEmpleadoDTO dto)
        {
            //Lo primero que hace es verificar que el rol exista antes de crear nada
            var existeRol = await roleManager.RoleExistsAsync(dto.Rol);
            if (!existeRol)
            {
                return ResultadoCrearEmpleado.ConError(
                    $"El rol '{dto.Rol}' no existe en el sistema.");
            }

            //verificar que el email no este ya registrado
            var usuarioExistente = await userManager.FindByEmailAsync(dto.Email);
            if (usuarioExistente != null)
            {
                return ResultadoCrearEmpleado.ConError(
                    $"Ya existe un usuario con el email '{dto.Email}'.");
            }

            var estrategia = context.Database.CreateExecutionStrategy();
            return await estrategia.ExecuteAsync(async () =>
            {
                context.ChangeTracker.Clear();
                await using var tx = await context.Database.BeginTransactionAsync();
                var nuevoUser = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    EmailConfirmed = true
                };
                var resultadoCrear = await userManager.CreateAsync(nuevoUser, dto.Contrasena);
                if (!resultadoCrear.Succeeded)
                    return ResultadoCrearEmpleado.ConErrores(resultadoCrear.Errors.Select(e => TraducirErrorIdentity(e.Code)));
                var rol = await userManager.AddToRoleAsync(nuevoUser, dto.Rol);
                if (!rol.Succeeded)
                    return ResultadoCrearEmpleado.ConErrores(rol.Errors.Select(e => TraducirErrorIdentity(e.Code)));
                var claim = await userManager.AddClaimAsync(nuevoUser, new System.Security.Claims.Claim("nombre", dto.Nombre));
                if (!claim.Succeeded)
                    return ResultadoCrearEmpleado.ConErrores(claim.Errors.Select(e => TraducirErrorIdentity(e.Code)));
                await GuardarContrasenaVisible(nuevoUser, dto.Contrasena);
                context.Usuarios.Add(new PericoOnFire_2026.BD.Datos.Entity.Usuario
                {
                    IdApplicationUser = nuevoUser.Id,
                    Nombre = dto.Nombre.Trim(),
                    Activo = true,
                    EstadoRegistro = EnumEstadoRegistro.activo
                });
                await context.SaveChangesAsync();
                await tx.CommitAsync();
                return ResultadoCrearEmpleado.Ok();
            });
        }

        //En este Task se le asigna un rol a un usario que ya existe
        public async Task<ResultadoOperacionSeguridad> AsignarRol(AsignarRolDTO dto)
        {
            try
            {
                var existeRol = await roleManager.RoleExistsAsync(dto.Rol);
                if (!existeRol)
                    return ResultadoOperacionSeguridad.Fallido;

                var usuario = await userManager.FindByEmailAsync(dto.Email);
                if (usuario == null)
                    return ResultadoOperacionSeguridad.NoEncontrado;

                // Verificar que no tenga ya ese rol para no duplicar
                var tieneRol = await userManager.IsInRoleAsync(usuario, dto.Rol);
                if (tieneRol)
                    return ResultadoOperacionSeguridad.Exitoso; // ya lo tiene, no es un error

                await userManager.AddToRoleAsync(usuario, dto.Rol);

                // UpdateSecurityStamp hace que la cookie del usuario se invalide
                // si estaba logueado, forzándolo a re-autenticarse con el nuevo rol
                await userManager.UpdateSecurityStampAsync(usuario);

                return ResultadoOperacionSeguridad.Exitoso;
            }
            catch (Exception)
            {
                return ResultadoOperacionSeguridad.Fallido;
            }
        }

        public async Task<ResultadoOperacionSeguridad> RemoverRol(AsignarRolDTO dto)
        {
            try
            {
                var usuario = await userManager.FindByEmailAsync(dto.Email);
                if (usuario == null)
                    return ResultadoOperacionSeguridad.NoEncontrado;

                var tieneRol = await userManager.IsInRoleAsync(usuario, dto.Rol);
                if (!tieneRol)
                    return ResultadoOperacionSeguridad.Exitoso; // no lo tiene, no es un error

                await userManager.RemoveFromRoleAsync(usuario, dto.Rol);
                await userManager.UpdateSecurityStampAsync(usuario);

                return ResultadoOperacionSeguridad.Exitoso;
            }
            catch (Exception)
            {
                return ResultadoOperacionSeguridad.Fallido;
            }
        }

        // Resultado específico para CrearEmpleado porque necesitamos
        // poder devolver los errores detallados de Identity, como por ejemplo contraseña débil,
        // email duplicado, y todo esos dramas y no solo un enum genérico.
        public class ResultadoCrearEmpleado
        {
            public bool Exitoso { get; set; }
            public List<string> Errores { get; set; } = new();

            // Método de fábrica para el caso exitoso
            public static ResultadoCrearEmpleado Ok() =>
                new ResultadoCrearEmpleado { Exitoso = true };

            // Método de fábrica para el caso con errores de Identity
            public static ResultadoCrearEmpleado ConErrores(IEnumerable<string> errores) =>
                new ResultadoCrearEmpleado { Exitoso = false, Errores = errores.ToList() };

            // Método de fábrica para un error simple de string
            public static ResultadoCrearEmpleado ConError(string error) =>
                new ResultadoCrearEmpleado { Exitoso = false, Errores = new List<string> { error } };
        }

        //Supuestamente Identity devuelve codigos en ingles, por lo que este metodo
        //traduce esos códigos a mensajes en español para que el admin entienda qué pasó
        private static string TraducirErrorIdentity(string codigo)
        {
            return codigo switch
            {
                "PasswordTooShort"
                    => "La contraseña es demasiado corta (mínimo 6 caracteres).",
                "PasswordRequiresNonAlphanumeric"
                    => "La contraseña debe tener al menos un carácter especial (ej: @, #, !).",
                "PasswordRequiresDigit"
                    => "La contraseña debe tener al menos un número.",
                "PasswordRequiresUpper"
                    => "La contraseña debe tener al menos una letra mayúscula.",
                "PasswordRequiresLower"
                    => "La contraseña debe tener al menos una letra minúscula.",
                "PasswordRequiresUniqueChars"
                    => "La contraseña debe tener más caracteres distintos.",
                "PasswordMismatch"
                    => "La contraseña actual no es correcta.",
                "DuplicateUserName"
                    => "Ya existe un usuario con ese nombre de usuario.",
                "DuplicateEmail"
                    => "Ya existe un usuario con ese email.",
                "InvalidEmail"
                    => "El formato del email no es válido.",
                "InvalidUserName"
                    => "El nombre de usuario contiene caracteres no permitidos.",
                _
                    => $"Error de Identity: {codigo}."
            };
        }

        public class ResultadoContrasena
        {
            public bool Exitoso { get; set; }
            public List<string> Errores { get; set; } = new();

            public static ResultadoContrasena Ok() => new ResultadoContrasena { Exitoso = true };
            public static ResultadoContrasena ConError(string error) =>
                new ResultadoContrasena { Exitoso = false, Errores = new List<string> { error } };
            public static ResultadoContrasena ConErrores(IEnumerable<string> errores) =>
                new ResultadoContrasena { Exitoso = false, Errores = errores.ToList() };
        }

        // Guarda (cifrada) la contraseña en texto para que el admin y el propio usuario puedan verla.
        // Si no hay clave de cifrado configurada, no hace nada.
        public async Task GuardarContrasenaVisible(ApplicationUser usuario, string contrasena)
        {
            if (!cifrado.Habilitado) return;
            var cifrada = cifrado.Cifrar(contrasena);
            if (cifrada == null) return;
            await userManager.SetAuthenticationTokenAsync(usuario, ProveedorToken, NombreToken, cifrada);
        }

        private async Task<string?> LeerContrasenaVisible(ApplicationUser usuario)
        {
            if (!cifrado.Habilitado) return null;
            var cifrada = await userManager.GetAuthenticationTokenAsync(usuario, ProveedorToken, NombreToken);
            return string.IsNullOrEmpty(cifrada) ? null : cifrado.Descifrar(cifrada);
        }

        public async Task<string?> ObtenerContrasenaVisiblePorEmail(string email)
        {
            var usuario = await userManager.FindByEmailAsync(email);
            return usuario == null ? null : await LeerContrasenaVisible(usuario);
        }

        public async Task<string?> ObtenerContrasenaVisiblePorId(string idUsuario)
        {
            var usuario = await userManager.FindByIdAsync(idUsuario);
            return usuario == null ? null : await LeerContrasenaVisible(usuario);
        }

        // El administrador define una contraseña nueva para otro usuario (cumple la misma política que al crearlo).
        public async Task<ResultadoContrasena> EstablecerContrasena(EstablecerContrasenaDTO dto)
        {
            var usuario = await userManager.FindByEmailAsync(dto.Email);
            if (usuario == null)
                return ResultadoContrasena.ConError($"No se encontró el usuario {dto.Email}.");

            var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
            var resultado = await userManager.ResetPasswordAsync(usuario, token, dto.ContrasenaNueva);
            if (!resultado.Succeeded)
                return ResultadoContrasena.ConErrores(resultado.Errors.Select(e => TraducirErrorIdentity(e.Code)));

            await GuardarContrasenaVisible(usuario, dto.ContrasenaNueva);
            return ResultadoContrasena.Ok();
        }

        // Cada usuario cambia su propia contraseña confirmando la actual.
        public async Task<ResultadoContrasena> CambiarContrasenaPropia(string idUsuario, CambiarMiContrasenaDTO dto)
        {
            var usuario = await userManager.FindByIdAsync(idUsuario);
            if (usuario == null)
                return ResultadoContrasena.ConError("No se encontró el usuario.");

            var resultado = await userManager.ChangePasswordAsync(usuario, dto.ContrasenaActual, dto.ContrasenaNueva);
            if (!resultado.Succeeded)
                return ResultadoContrasena.ConErrores(resultado.Errors.Select(e => TraducirErrorIdentity(e.Code)));

            await GuardarContrasenaVisible(usuario, dto.ContrasenaNueva);
            return ResultadoContrasena.Ok();
        }

        public async Task<ResultadoOperacionSeguridad> DesactivarUsuario(string email)
        {
            try
            {
                var usuario = await userManager.FindByEmailAsync(email);
                if (usuario == null)
                    return ResultadoOperacionSeguridad.NoEncontrado;

                // LockoutEnd en el futuro lejano = bloqueado indefinidamente
                await userManager.SetLockoutEnabledAsync(usuario, true);
                await userManager.SetLockoutEndDateAsync(usuario, DateTimeOffset.MaxValue);
                await userManager.UpdateSecurityStampAsync(usuario);
                return ResultadoOperacionSeguridad.Exitoso;
            }
            catch { return ResultadoOperacionSeguridad.Fallido; }
        }

        public async Task<ResultadoOperacionSeguridad> ActivarUsuario(string email)
        {
            try
            {
                var usuario = await userManager.FindByEmailAsync(email);
                if (usuario == null)
                    return ResultadoOperacionSeguridad.NoEncontrado;

                // Sacar el lockout = usuario activo de nuevo
                await userManager.SetLockoutEndDateAsync(usuario, null);
                await userManager.UpdateSecurityStampAsync(usuario);
                return ResultadoOperacionSeguridad.Exitoso;
            }
            catch { return ResultadoOperacionSeguridad.Fallido; }
        }
    }
}
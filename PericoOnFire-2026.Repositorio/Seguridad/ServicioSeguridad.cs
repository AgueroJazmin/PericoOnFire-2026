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

        public ServicioSeguridad(MiDbContext context,
                                 UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this.context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        public async Task<List<UsuarioRolDTO>> ObtenerUsuarios()
        {
            var usuarios = await context.Users
                .OrderBy(u => u.UserName)
                .AsNoTracking()
                .ToListAsync();

            var resultado = new List<UsuarioRolDTO>();

            foreach (var u in usuarios)
            {
                // GetRolesAsync consulta la tabla AspNetUserRoles de Identity
                var roles = await userManager.GetRolesAsync(u);

                resultado.Add(new UsuarioRolDTO
                {
                    Id = u.Id,
                    Email = u.Email!,
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

            //Armar el ApplicationUser
            //EmailConfirmed = true es importante ya que saltea el paso de confirmación
            //porque el admin ya validó al empleado, por lo que no necesita confirmar nada.
            var nuevoUser = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true
            };

            // 4. CreateAsync hashea la contraseña automáticamente
            //    Si la contraseña no cumple las reglas de Identity (longitud,
            //    mayúsculas, etc.) nos devuelve los errores detallados.
            var resultadoCrear = await userManager.CreateAsync(nuevoUser, dto.Contrasena);

            if (!resultadoCrear.Succeeded)
            {
                // Traducimos los errores de Identity a español para que el admin
                // entienda qué salió mal (ej: "Passwords must have at least one digit")
                var errores = resultadoCrear.Errors
                    .Select(e => TraducirErrorIdentity(e.Code))
                    .ToList();

                return ResultadoCrearEmpleado.ConErrores(errores);
            }

            // 5. Asignar el rol en el mismo acto que se crea la cuenta
            await userManager.AddToRoleAsync(nuevoUser, dto.Rol);

            return ResultadoCrearEmpleado.Ok();
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

using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class UsuarioRolDTO
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string Nombre { get; set; } = "";

        // true si el sistema guardó (cifrada) la contraseña de este usuario y el admin puede verla.
        // Los usuarios creados antes de este cambio no la tienen hasta que se les asigne una nueva.
        public bool TieneContrasenaVisible { get; set; }

        public List<string> Roles { get; set; } = new();

        public bool Activo { get; set; } = true;

    }
}
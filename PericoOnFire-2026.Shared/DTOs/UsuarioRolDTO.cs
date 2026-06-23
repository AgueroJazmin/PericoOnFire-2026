using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class UsuarioRolDTO
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public List<string> Roles { get; set; } = new();

        public bool Activo { get; set; } = true;

    }
}

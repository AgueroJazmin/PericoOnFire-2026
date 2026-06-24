using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class UsuarioEditarDTO
    {
        [Required]
        public string Email { get; set; } = "";

        [Required]
        public string Rol { get; set; } = "";

        public bool EmailConfirmed { get; set; }
    }
}

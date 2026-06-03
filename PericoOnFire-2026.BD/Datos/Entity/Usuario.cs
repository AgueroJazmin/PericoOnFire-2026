using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    public class Usuario : EntityBase
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El usuario es obligatorio")]
        [MaxLength(50)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio")]
        public EnumRolUsuario Rol { get; set; }

        public bool Activo { get; set; } = true;
    }
}

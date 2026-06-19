using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    public class Usuario : EntityBase
    {
        //Esto vendria a ser la FK al aplication user 
        [Required]
        public string IdApplicationUser { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;

        public ApplicationUser ApplicationUser { get; set; } = null!;


        /* Saco esto de Usuario porque el identity ya lo genera propio
           Tambien es necesario modificar el EnumRolUsuario porque el identity ya tiene su propio sistema de roles
           
        
        [Required(ErrorMessage = "El usuario es obligatorio")]
        [MaxLength(50)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio")]
        public EnumRolUsuario Rol { get; set; } 
         */
    }
}

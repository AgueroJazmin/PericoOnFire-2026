using System.ComponentModel.DataAnnotations;

namespace PericoOnFire_2026.Shared.DTOs
{
    // Respuesta al consultar una contraseña (la del propio usuario o, siendo admin, la de otro).
    public class ContrasenaVisibleDTO
    {
        public string Contrasena { get; set; } = "";
    }

    // El administrador define una contraseña nueva para otro usuario.
    public class EstablecerContrasenaDTO
    {
        [Required]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "La contraseña nueva es obligatoria")]
        [MinLength(6, ErrorMessage = "Mínimo 6 caracteres")]
        public string ContrasenaNueva { get; set; } = "";
    }

    // Cada usuario cambia su propia contraseña (hay que confirmar la actual).
    public class CambiarMiContrasenaDTO
    {
        [Required(ErrorMessage = "Ingresá tu contraseña actual")]
        public string ContrasenaActual { get; set; } = "";

        [Required(ErrorMessage = "La contraseña nueva es obligatoria")]
        [MinLength(6, ErrorMessage = "Mínimo 6 caracteres")]
        public string ContrasenaNueva { get; set; } = "";
    }
}
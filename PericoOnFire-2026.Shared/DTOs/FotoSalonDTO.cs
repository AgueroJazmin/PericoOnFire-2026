using System.ComponentModel.DataAnnotations;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class FotoSalonDTO
    {
        [Required]
        public string ContenidoBase64 { get; set; } = "";

        [Required]
        public string MimeType { get; set; } = "";

        public string NombreArchivo { get; set; } = "foto-salon";
    }
}

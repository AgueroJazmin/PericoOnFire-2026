using System.ComponentModel.DataAnnotations;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    public class ConfiguracionSalon : EntityBase
    {
        public byte[] Foto { get; set; } = Array.Empty<byte>();

        [MaxLength(100)]
        public string MimeType { get; set; } = "image/jpeg";

        [MaxLength(200)]
        public string NombreArchivo { get; set; } = "foto-salon.jpg";
    }
}

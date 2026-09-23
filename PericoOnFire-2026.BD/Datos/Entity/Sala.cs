using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    // Representa un ambiente del local, es decir si será Salón, Terraza, Exterior, etc.
    // dentro del cual se distribuyen las mesas en una grilla de filas/columnas.
    public class Sala : EntityBase
    {
        [Required(ErrorMessage = "El nombre de la sala es obligatorio")]
        [MaxLength(60)]
        public string Nombre { get; set; } = "";

        // Define el orden de las solapas/secciones en la configuración y en la page de Mesas.
        public int Orden { get; set; } = 0;
        public List<Mesa> Mesas { get; set; } = new();
    }
}

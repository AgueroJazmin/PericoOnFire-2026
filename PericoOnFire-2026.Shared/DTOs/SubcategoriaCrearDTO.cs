using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class SubcategoriaCrearDTO
    {
        [Required]
        public int IdCategoria { get; set; }

        [Required]
        [MaxLength(100)]
        public string NombreSubcategoria { get; set; } = "";
    }
}

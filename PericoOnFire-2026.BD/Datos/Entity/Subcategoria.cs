using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    public class Subcategoria : EntityBase
    {

        [Required(ErrorMessage = "La categoría es obligatoria")]
        public int IdCategoria { get; set; }

        [Required(ErrorMessage = "El nombre de la subcategoría es obligatorio")]
        [MaxLength(100, ErrorMessage = "El nombre excede la cantidad de caracteres permitidos")]
        public string NombreSubcategoria { get; set; } = string.Empty;

        public Categoria Categoria { get; set; } = null!;
    }
}

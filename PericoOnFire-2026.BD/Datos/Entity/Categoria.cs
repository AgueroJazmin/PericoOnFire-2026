using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    public class Categoria : EntityBase
    {
        [Required(ErrorMessage = "El Nombre de la categoría es obligatorio")]
        [MaxLength(100, ErrorMessage = "El Nombre excede la cantidad de caracteres permitidos")]
        public string NombreCategoria { get; set; } = string.Empty;

        public List<Subcategoria> Subcategorias { get; set; } = new();
    }
}

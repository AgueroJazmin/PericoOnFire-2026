using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class SubcategoriaListadoDTO
    {
        public int Id { get; set; }
        public int IdCategoria { get; set; }
        public string NombreSubcategoria { get; set; } = "";
        public string NombreCategoria { get; set; } = "";
    }
}

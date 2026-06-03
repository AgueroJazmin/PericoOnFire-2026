using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    public class Producto : EntityBase
    {
        [Required(ErrorMessage = "El Nombre del producto es obligatorio")]
        [MaxLength(100, ErrorMessage = "El Nombre excede la cantidad de caracteres permitidos")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El Precio del producto es obligatorio")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "La subcategoría es obligatoria")]
        public int IdSubcategoria { get; set; }

        public EnumSectorDestino SectorDestino { get; set; }

        public bool Activo { get; set; } = true;

        public Subcategoria Subcategoria { get; set; } = null!;

        public List<DetallePedido> DetallesPedido { get; set; } = new();
    }
}

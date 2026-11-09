using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    // Un producto que forma parte de una promo, con la cantidad que trae cada promo.
    public class ComponentePromoDTO
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = "";
        public int Cantidad { get; set; } = 1;
    }
}
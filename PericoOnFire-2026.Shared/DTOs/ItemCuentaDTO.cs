using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class ItemCuentaDTO
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public string? Observacion { get; set; }
        public decimal Subtotal => PrecioUnitario * Cantidad;
    }
}

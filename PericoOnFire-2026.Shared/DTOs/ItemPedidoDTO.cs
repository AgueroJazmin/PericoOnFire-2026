using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class ItemPedidoDTO
    {
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public string? Observacion { get; set; }
    }
}

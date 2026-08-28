using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class DetallePedidoDTO
    {
        public int Id { get; set; }
        public int IdPedido { get; set; }
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public string? Observacion { get; set; }
        public string NombreProducto { get; set; } = ""; //para no tener que armar otra clase solo para mostrar el nombre

    }
}

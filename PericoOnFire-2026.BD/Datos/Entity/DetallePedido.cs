using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    public class DetallePedido : EntityBase
    {
        public int IdPedido { get; set; }

        public int IdProducto { get; set; }

        [Range(1, 999, ErrorMessage = "La cantidad debe ser mayor a cero")]
        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        [MaxLength(300)]
        public string? Observacion { get; set; }

        public Pedido Pedido { get; set; } = null!;

        public Producto Producto { get; set; } = null!;
    }
}

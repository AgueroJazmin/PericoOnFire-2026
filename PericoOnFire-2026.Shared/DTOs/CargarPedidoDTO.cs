using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class CargarPedidoDTO
    {
        public int IdComanda { get; set; }
        public List<ItemPedidoDTO> Items { get; set; } = new();
    }
}

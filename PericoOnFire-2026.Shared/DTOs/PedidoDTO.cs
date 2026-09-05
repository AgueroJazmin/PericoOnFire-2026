using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class PedidoDTO
    {
        public int Id { get; set; }
        public int IdComanda { get; set; }
        public EnumSectorDestino SectorDestino { get; set; }
        public EnumEstadoPedido Estado { get; set; }
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaInicioPreparacion { get; set; }
        public DateTime? FechaListo { get; set; }
        public DateTime? FechaEntregado { get; set; }
        public int? IdDelivery { get; set; }
        public string? Observaciones { get; set; }
        public int? NumeroMesa { get; set; }
        public EnumTipoServicio? TipoServicio { get; set; }
        public string? MotivoCancelacion { get; set; }
        public DateTime? FechaCancelado { get; set; }
        public string NumeroComanda => $"COM-{IdComanda:D6}";
        public List<DetallePedidoDTO> DetallesPedido { get; set; } = new();
    }
}

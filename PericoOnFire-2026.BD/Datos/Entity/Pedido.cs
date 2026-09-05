using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.BD.Datos.Entity
{
        public class Pedido : EntityBase
        {
            public int IdComanda { get; set; }
            public EnumSectorDestino SectorDestino { get; set; }
            public EnumEstadoPedido Estado { get; set; } = EnumEstadoPedido.Pendiente;
            public DateTime FechaPedido { get; set; } = DateTime.UtcNow;
            public DateTime? FechaInicioPreparacion { get; set; }
            public DateTime? FechaListo { get; set; }
            public DateTime? FechaEntregado { get; set; }
            public int? IdDelivery { get; set; }

            [MaxLength(300)]
            public string? Observaciones { get; set; }
            public Comanda Comanda { get; set; } = null!;
            public Usuario? Delivery { get; set; }
            
            [MaxLength(300)]
            public string? MotivoCancelacion { get; set; }
            public DateTime? FechaCancelado { get; set; }
            public List<DetallePedido> DetallesPedido { get; set; } = new();
    }
    
}

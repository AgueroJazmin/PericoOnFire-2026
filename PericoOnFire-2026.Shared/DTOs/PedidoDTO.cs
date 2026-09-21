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
        public DateTime? HoraDeseada { get; set; }
        public string NumeroComanda => $"COM-{IdComanda:D6}";

        //Le agrego estas dos propiedades para que el front pueda mostrar el nombre del cliente
        //y la direccion en la comanda, sin tener que hacer un join con la tabla Cliente.
        public string? NombreCliente { get; set; }
        public string? Direccion { get; set; }
        public List<DetallePedidoDTO> DetallesPedido { get; set; } = new();

        //Ítems de la misma comanda que fueron a otro sector, en este caso seria la bebida que le
        //corresponde a Barra, mostrada en gris en la comanda de Cocina como referencia,
        //sin botones de acción sobre ellos
        public List<DetallePedidoDTO> DetallesOtroSector { get; set; } = new();
    }
}

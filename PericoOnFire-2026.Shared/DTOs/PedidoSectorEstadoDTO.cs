using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class PedidoSectorEstadoDTO
    {
        public int IdPedido { get; set; }
        public EnumSectorDestino Sector { get; set; }
        public EnumEstadoPedido Estado { get; set; }
    }
}

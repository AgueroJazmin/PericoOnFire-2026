using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class PedidoCrearDTO
    {
        public int IdComanda { get; set; }

        public EnumSectorDestino SectorDestino { get; set; }

        public string? Observaciones { get; set; }
    }
}

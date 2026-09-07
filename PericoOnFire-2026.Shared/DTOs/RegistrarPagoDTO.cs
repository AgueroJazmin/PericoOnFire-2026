using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class RegistrarPagoDTO
    {
        public int IdComanda { get; set; }
        public int IdUsuarioCaja { get; set; }
        public EnumTipoPago TipoPago { get; set; }
        public decimal MontoPagado { get; set; }
    }
}

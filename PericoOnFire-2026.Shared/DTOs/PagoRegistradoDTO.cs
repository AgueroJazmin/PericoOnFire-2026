using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class PagoRegistradoDTO
    {
        public int IdPago { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal Vuelto { get; set; }
    }
}

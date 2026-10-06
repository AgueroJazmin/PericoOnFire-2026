using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class ResumenVentasDTO
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public decimal TotalVendido { get; set; }
        public int CantidadVentas { get; set; }
        public decimal TicketPromedio => CantidadVentas == 0 ? 0 : Math.Round(TotalVendido / CantidadVentas, 2);
        public List<TotalPorConceptoDTO> PorTipoPago { get; set; } = new();
        public List<TotalPorConceptoDTO> PorTipoServicio { get; set; } = new();
        public List<TotalPorConceptoDTO> PorDia { get; set; } = new();
        public List<TotalPorConceptoDTO> ProductosMasVendidos { get; set; } = new();
    }
}

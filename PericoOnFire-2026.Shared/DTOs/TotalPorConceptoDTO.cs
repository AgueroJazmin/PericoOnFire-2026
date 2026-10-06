using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    //Fila genérica de las estadísticas: forma de pago, tipo de servicio, día o producto.
    public class TotalPorConceptoDTO
    {
        public string Concepto { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal Monto { get; set; }
    }
}

using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class MovimientoCajaCrearDTO
    {
        public EnumTipoMovCaja TipoMovimiento { get; set; } = EnumTipoMovCaja.Ingreso;
        public decimal Monto { get; set; }
        public string Motivo { get; set; } = "";
        public string? Observaciones { get; set; }
    }
}

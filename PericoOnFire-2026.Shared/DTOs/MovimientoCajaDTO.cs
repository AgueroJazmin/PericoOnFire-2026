using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class MovimientoCajaDTO
    {
        public int Id { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public EnumTipoMovCaja TipoMovimiento { get; set; }
        public decimal Monto { get; set; }
        public string Motivo { get; set; } = "";
        public string? Observaciones { get; set; }
        public string? NombreUsuario { get; set; }

        //Apertura e ingreso suman al cajón; egreso y retiro restan.
        public bool EsEntrada => TipoMovimiento == EnumTipoMovCaja.Apertura || TipoMovimiento == EnumTipoMovCaja.Ingreso;
        public bool EsSalida => TipoMovimiento == EnumTipoMovCaja.Egreso || TipoMovimiento == EnumTipoMovCaja.Retiro;
    }
}

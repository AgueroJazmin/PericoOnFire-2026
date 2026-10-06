using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class TurnoCajaDTO
    {
        public int Id { get; set; }
        public EnumEstadoTurnoCaja Estado { get; set; }
        public bool Abierto => Estado == EnumEstadoTurnoCaja.Abierto;
        public DateTime FechaApertura { get; set; }
        public string? NombreUsuarioApertura { get; set; }
        public decimal MontoInicial { get; set; }
        public DateTime? FechaCierre { get; set; }
        public string? NombreUsuarioCierre { get; set; }
        public decimal? MontoEsperado { get; set; }
        public decimal? MontoContado { get; set; }
        public decimal? Diferencia { get; set; }
        public string? ObservacionesCierre { get; set; }
    }
}

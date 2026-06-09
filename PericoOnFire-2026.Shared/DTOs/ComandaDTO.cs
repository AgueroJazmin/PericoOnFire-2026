using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class ComandaDTO
    {
        public int Id { get; set; }

        public int? IdMesa { get; set; }

        public int? IdCliente { get; set; }

        public int? IdUsuario { get; set; }

        public EnumTipoServicio TipoServicio { get; set; }

        public EnumEstadoComanda Estado { get; set; }

        public DateTime FechaApertura { get; set; }

        public DateTime? FechaCierre { get; set; }

        public decimal Total { get; set; }

        public string? Observaciones { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    public class MovimientoCaja : EntityBase
    {
        public int IdUsuario { get; set; }

        public EnumTipoMovCaja TipoMovimiento { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "El motivo es obligatorio")]
        [MaxLength(200)]
        public string Motivo { get; set; } = string.Empty;

        public DateTime FechaMovimiento { get; set; } = DateTime.UtcNow;

        [MaxLength(300)]
        public string? Observaciones { get; set; }

        public Usuario Usuario { get; set; } = null!;
    }
}

using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    public class Pago : EntityBase
    {
        [Required(ErrorMessage = "La comanda es obligatoria")]
        public int IdComanda { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio")]
        public int IdUsuarioCaja { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un tipo de pago")]
        public EnumTipoPago TipoPago { get; set; }

        [Range(0.01, double.MaxValue,
        ErrorMessage = "El total debe ser mayor a cero")]
        public decimal MontoTotal { get; set; }

        [Range(0.01, double.MaxValue,
        ErrorMessage = "El monto debe ser mayor a cero")]
        public decimal MontoPagado { get; set; }

        public decimal Vuelto { get; set; }

        public DateTime FechaPago { get; set; } = DateTime.UtcNow;

        public Comanda Comanda { get; set; } = null!;

        public Usuario UsuarioCaja { get; set; } = null!;
    }
}

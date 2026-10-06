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

        //Total de la cuenta.
        [Range(0.01, double.MaxValue,
        ErrorMessage = "El total debe ser mayor a cero")]
        public decimal MontoTotal { get; set; }

        //Con el pago combinado una comanda puede tener varias filas de Pago (una por forma de pago).
        //MontoPagado es lo que se recibió con ESA forma de pago y Vuelto solo se carga en la fila de
        //efectivo, así que lo que quedó aplicado a la cuenta es MontoPagado - Vuelto.
        [Range(0.01, double.MaxValue,
        ErrorMessage = "El monto debe ser mayor a cero")]
        public decimal MontoPagado { get; set; }
        public decimal Vuelto { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.UtcNow;

        //Turno de caja en el que se cobró. Es null en los pagos anteriores a que existieran los turnos.
        public int? IdTurnoCaja { get; set; }
        public Comanda Comanda { get; set; } = null!;
        public Usuario UsuarioCaja { get; set; } = null!;
        public TurnoCaja? TurnoCaja { get; set; }
    }
}

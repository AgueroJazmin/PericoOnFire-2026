using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    public class TurnoCaja : EntityBase
    {
        //Un turno de caja va desde que se abre con un monto inicial hasta que se cierra con el arqueo.
        //Los cobros (Pago) y los movimientos (MovimientoCaja) se cargan siempre sobre el turno abierto,
        //así el arqueo sabe exactamente qué dinero entró y salió durante ese turno.
        public EnumEstadoTurnoCaja Estado { get; set; } = EnumEstadoTurnoCaja.Abierto;
        public int IdUsuarioApertura { get; set; }
        public DateTime FechaApertura { get; set; } = DateTime.UtcNow;

        //Efectivo con el que arranca el cajón (el cambio disponible).
        public decimal MontoInicial { get; set; }
        public int? IdUsuarioCierre { get; set; }
        public DateTime? FechaCierre { get; set; }

        //Datos del arqueo, se completan recién al cerrar:
        //esperado = lo que el sistema calcula, contado = lo que se contó en el cajón.
        public decimal? MontoEsperado { get; set; }
        public decimal? MontoContado { get; set; }

        //Contado menos esperado: positivo es sobrante, negativo es faltante.
        public decimal? Diferencia { get; set; }

        [MaxLength(300)]
        public string? ObservacionesCierre { get; set; }
        public Usuario UsuarioApertura { get; set; } = null!;
        public Usuario? UsuarioCierre { get; set; }
        public List<Pago> Pagos { get; set; } = new();
        public List<MovimientoCaja> Movimientos { get; set; } = new();
    }
}

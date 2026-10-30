using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class RegistrarPagoDTO
    {
        //Pago combinado: la cuenta se puede cobrar con varias formas de pago a la vez
        //(por ej. la mitad en efectivo y el resto con QR o transferencia).
        public int IdComanda { get; set; }
        public int IdUsuarioCaja { get; set; }
        public List<LineaPagoDTO> Lineas { get; set; } = new();

        //Estos dos son los que agrego para que se pueda aplicar un descuento a la cuenta. Se aplicará sobre el total de la cuenta.
        public EnumTipoDescuento TipoDescuento { get; set; } = EnumTipoDescuento.Ninguno;
        public decimal ValorDescuento { get; set; } // % o $ según TipoDescuento
    }
}

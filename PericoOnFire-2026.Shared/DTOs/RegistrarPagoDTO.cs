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
    }
}

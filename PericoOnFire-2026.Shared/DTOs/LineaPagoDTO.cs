using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class LineaPagoDTO
    {
        //Una parte del cobro de una cuenta. Al cobrar, Monto es lo que el cliente entregó con esa
        //forma de pago; en las ventas ya registradas, Monto es lo que quedó aplicado a la cuenta
        //(en efectivo, ya descontado el vuelto).
        public EnumTipoPago TipoPago { get; set; }
        public decimal Monto { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.ENUM
{
    //Formas de pago que caja ofrece hoy al cobrar
    public static class TiposPagoCaja
    {
        public static readonly EnumTipoPago[] Habilitados =
        {
            EnumTipoPago.Efectivo,
            EnumTipoPago.Debito,
            EnumTipoPago.Credito,
            EnumTipoPago.Transferencia,
            EnumTipoPago.QR
        };

        //Para que se vean en pantalla 
        public static string Nombre(EnumTipoPago tipo) => tipo switch
        {
            EnumTipoPago.Debito => "Débito",
            EnumTipoPago.Credito => "Crédito",
            EnumTipoPago.MercadoPago => "Mercado Pago",
            _ => tipo.ToString()
        };
    }
}

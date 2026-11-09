using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.ENUM
{
    public enum EnumTipoMovCaja
    {
        Apertura = 1,
        Ingreso = 2,
        Egreso = 3,
        Retiro = 4,
        Ajuste = 5,
        Cierre = 6,

        // Devolución a un cliente por un medio que no es efectivo, no mueve el cajón.
        // Esto es para poder registrar devoluciones de tarjetas de crédito, por ejemplo.
        DevolucionSinEfectivo = 7
    }
}

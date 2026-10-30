using PericoOnFire_2026.Shared.ENUM;
using System;

namespace PericoOnFire_2026.Shared.Utilidades
{
    public static class CalculoDescuento
    {
        // Devuelve el monto en $ a descontar. Si no es válido devuelve 0 y completa error.
        // El total con descuento siempre queda mayor a cero (Pago.MontoTotal lo exige).
        public static decimal Calcular(decimal subtotal, EnumTipoDescuento tipo, decimal valor, out string? error)
        {
            error = null;
            if (tipo == EnumTipoDescuento.Ninguno) return 0;
            if (valor <= 0) { error = "Ingresá un descuento mayor a cero."; return 0; }

            decimal monto;
            if (tipo == EnumTipoDescuento.Porcentaje)
            {
                if (valor >= 100) { error = "El porcentaje debe ser menor a 100."; return 0; }
                monto = Math.Round(subtotal * valor / 100m, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                monto = Math.Round(valor, 2, MidpointRounding.AwayFromZero);
            }

            if (monto >= subtotal) { error = "El descuento no puede igualar o superar el total de la cuenta."; return 0; }
            return monto;
        }
    }
}
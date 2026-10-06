using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.Utilidades
{
    public class ResultadoCobro
    {
        public string? Error { get; init; }
        public bool EsValido => Error == null;
        public decimal TotalCobrado { get; init; }
        public decimal Vuelto { get; init; }
        public List<LineaPagoDTO> Lineas { get; init; } = new();
    }

    //En esta parte se usa el PagoController.Registrar para validar de
    //verdad y el cliente (Caja.razor) para avisar antes de enviar, así los dos aplican lo mismo.
    //Solo el efectivo da vuelto, el resto de lo cobrado con transferencia o QR o qsy lo que sea no puede pasarse del total,
    //porque ese excedente nunca saldría de caja y descuadraría el arqueo.
    public static class CobroCombinado
    {
        public static ResultadoCobro Evaluar(decimal total, IEnumerable<LineaPagoDTO>? lineas)
        {
            var normalizadas = (lineas ?? Enumerable.Empty<LineaPagoDTO>())
                .Where(l => l.Monto > 0)
                .GroupBy(l => l.TipoPago)
                .Select(g => new LineaPagoDTO { TipoPago = g.Key, Monto = Math.Round(g.Sum(l => l.Monto), 2) })
                .ToList();

            if (!normalizadas.Any())
                return Rechazar("Cargá al menos una forma de pago con su monto.");

            var noHabilitada = normalizadas.FirstOrDefault(l => !TiposPagoCaja.Habilitados.Contains(l.TipoPago));
            if (noHabilitada != null)
                return Rechazar($"La forma de pago {TiposPagoCaja.Nombre(noHabilitada.TipoPago)} no está habilitada.");

            var efectivo = normalizadas.Where(l => l.TipoPago == EnumTipoPago.Efectivo).Sum(l => l.Monto);
            var otros = normalizadas.Where(l => l.TipoPago != EnumTipoPago.Efectivo).Sum(l => l.Monto);
            var cobrado = efectivo + otros;

            if (otros > total)
                return Rechazar("Lo cobrado con tarjeta, transferencia o QR no puede superar el total de la cuenta: solo el efectivo da vuelto.");

            if (cobrado < total)
                return Rechazar($"El monto cobrado (${cobrado:0.00}) es menor al total de la cuenta (${total:0.00}).");

            return new ResultadoCobro
            {
                Lineas = normalizadas,
                TotalCobrado = cobrado,
                Vuelto = cobrado - total
            };
        }

        private static ResultadoCobro Rechazar(string error) => new() { Error = error };
    }
}

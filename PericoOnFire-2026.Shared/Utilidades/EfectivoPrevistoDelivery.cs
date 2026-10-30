using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PericoOnFire_2026.Shared.Utilidades
{
    // Guarda una instrucción de entrega, nunca un pago. Aprovecha el campo
    // Observaciones existente para no requerir migraciones antes de la demo.
    public static class EfectivoPrevistoDelivery
    {
        private const string Prefijo = "[EFECTIVO_DELIVERY:";
        private static readonly Regex Patron = new Regex(
            @"\[EFECTIVO_DELIVERY:(\d+(?:\.\d{1,2})?)\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string? Guardar(string? observaciones, decimal? efectivo)
        {
            if (!efectivo.HasValue) return observaciones;
            var texto = Prefijo + efectivo.Value.ToString("0.00", CultureInfo.InvariantCulture) + "]";
            return string.IsNullOrWhiteSpace(observaciones) ? texto : observaciones.Trim() + "\n" + texto;
        }

        // Saca la previsión de efectivo de las observaciones (se usa al pasar un Delivery a Take Away).
        public static string? Quitar(string? observaciones)
        {
            if (string.IsNullOrWhiteSpace(observaciones)) return observaciones;
            var limpio = Patron.Replace(observaciones, "").Trim();
            return string.IsNullOrWhiteSpace(limpio) ? null : limpio;
        }

        public static decimal? Leer(string? observaciones)
        {
            if (string.IsNullOrWhiteSpace(observaciones)) return null;
            var m = Patron.Match(observaciones);
            return m.Success && decimal.TryParse(m.Groups[1].Value,
                NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor)
                ? valor : null;
        }
    }
}
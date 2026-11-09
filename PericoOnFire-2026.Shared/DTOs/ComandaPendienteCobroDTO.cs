using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class ComandaPendienteCobroDTO
    {
        public int Id { get; set; }
        public int NumeroDiario { get; set; }
        public string NumeroComanda => $"COM-{NumeroDiario:D3}";
        public int? NumeroMesa { get; set; }
        public EnumTipoServicio TipoServicio { get; set; }
        public DateTime FechaApertura { get; set; }
        public int CantidadComensales { get; set; }
        public string? NombreMozo { get; set; }
        public List<ItemCuentaDTO> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Subtotal);

        // Si el pedido se editó después de cobrarlo, Caja cobra solo la diferencia.
        public decimal Descuento { get; set; }
        public decimal YaPagado { get; set; }
        public decimal SaldoACobrar => Math.Max(0, Total - Descuento - YaPagado);
    }
}

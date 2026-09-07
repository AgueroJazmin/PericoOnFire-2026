using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class ComandaPendienteCobroDTO
    {
        public int Id { get; set; }
        public string NumeroComanda => $"COM-{Id:D6}";
        public int? NumeroMesa { get; set; }
        public EnumTipoServicio TipoServicio { get; set; }
        public DateTime FechaApertura { get; set; }
        public int CantidadComensales { get; set; }
        public string? NombreMozo { get; set; }
        public List<ItemCuentaDTO> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Subtotal);
    }
}

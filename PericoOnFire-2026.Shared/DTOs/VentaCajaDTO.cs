using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    //Una venta es una cuenta (comanda) ya cobrada, con las formas de pago que se usaron.
    //Una venta es una cuenta (comanda) ya cobrada, con las formas de pago que se usaron.
    public class VentaCajaDTO
    {
        public int IdComanda { get; set; }
        public int NumeroDiario { get; set; }
        public string NumeroComanda => $"COM-{NumeroDiario:D3}";
        public EnumTipoServicio TipoServicio { get; set; }
        public int? NumeroMesa { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal Total { get; set; }
        public decimal Vuelto { get; set; }
        public string? NombreCajero { get; set; }
        public List<LineaPagoDTO> Pagos { get; set; } = new();
    }

}

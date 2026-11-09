using PericoOnFire_2026.Shared.ENUM;
using System;

namespace PericoOnFire_2026.Shared.DTOs
{
    // Comanda a la que Caja todavía le tiene que devolver plata al cliente.
    public class DevolucionPendienteDTO
    {
        public int IdComanda { get; set; }
        public int NumeroDiario { get; set; }
        public string NumeroComanda => $"COM-{NumeroDiario:D3}";
        public EnumTipoServicio TipoServicio { get; set; }
        public string? NombreCliente { get; set; }
        public decimal Monto { get; set; }
        public string Motivo { get; set; } = "";
    }

    public class RegistrarDevolucionDTO
    {
        public int IdComanda { get; set; }
        // Medio con el que se le devuelve la plata al cliente.
        public EnumTipoPago TipoPago { get; set; } = EnumTipoPago.Efectivo;
    }
}
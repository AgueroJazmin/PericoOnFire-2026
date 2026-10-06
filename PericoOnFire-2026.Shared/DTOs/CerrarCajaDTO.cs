using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class CerrarCajaDTO
    {
        //Efectivo que se contó físicamente en el cajón al cerrar el turno.
        public decimal MontoContado { get; set; }
        public string? Observaciones { get; set; }
    }
}

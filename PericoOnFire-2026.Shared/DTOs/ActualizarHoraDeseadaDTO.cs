using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class ActualizarHoraDeseadaDTO
    {
        //Null = "sin horario particular, lo antes posible".
        public DateTime? HoraDeseada { get; set; }
    }
}

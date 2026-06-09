using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class MesaDTO
    {
        public int Id { get; set; }

        public int NumeroMesa { get; set; }

        public EnumEstadoMesa Estado { get; set; }
    }
}

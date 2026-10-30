using PericoOnFire_2026.Shared.ENUM;
using System;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class CambiarTipoServicioDTO
    {
        public EnumTipoServicio NuevoTipo { get; set; }

        // Obligatoria al pasar a Delivery.
        public string? Direccion { get; set; }

        // Al pasar a Take Away se confirma el nombre de quien lo retira.
        public string? NombreCliente { get; set; }
    }
}
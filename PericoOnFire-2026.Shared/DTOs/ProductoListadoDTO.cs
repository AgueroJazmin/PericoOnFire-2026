using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class ProductoListadoDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public decimal Precio { get; set; }
        public int IdSubcategoria { get; set; }
        public string NombreSubcategoria { get; set; } = "";
        public EnumSectorDestino SectorDestino { get; set; }
        public bool Activo { get; set; }
    }
}

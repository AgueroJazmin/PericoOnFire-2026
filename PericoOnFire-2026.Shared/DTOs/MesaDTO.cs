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
        public bool TienePedidoListo { get; set; }
        public bool TienePedidoCancelado { get; set; }
        public int? IdSala { get; set; }
        public string? NombreSala { get; set; }
        public int Fila { get; set; }
        public int Columna { get; set; }
        public EnumFormaMesa Forma { get; set; } = EnumFormaMesa.Cuadrada;
        public EnumTamanioMesa Tamanio { get; set; } = EnumTamanioMesa.Mediana;
    }
}

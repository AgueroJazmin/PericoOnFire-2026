using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    // Se usa al hacer click en una celda vacía de la grilla de una sala,
    // el número de mesa se asigna solo (siguiente disponible), acá solo se
    // manda dónde va ubicada.
    public class MesaCrearDTO
    {
        public int IdSala { get; set; }
        public int Fila { get; set; }
        public int Columna { get; set; }
        public EnumFormaMesa Forma { get; set; } = EnumFormaMesa.Cuadrada;
        public EnumTamanioMesa Tamanio { get; set; } = EnumTamanioMesa.Mediana;
    }
}

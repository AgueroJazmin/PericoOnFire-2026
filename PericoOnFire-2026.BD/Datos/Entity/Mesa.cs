using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    public class Mesa : EntityBase
    {
        [Required(ErrorMessage = "El número de mesa es obligatorio")]
        public int NumeroMesa { get; set; }
        public EnumEstadoMesa Estado { get; set; } = EnumEstadoMesa.Libre;

        // A qué sala/ambiente pertenece y en qué celda de su grilla está ubicada.
        public int? IdSala { get; set; }

        public int Fila { get; set; } = 0;

        public int Columna { get; set; } = 0;

        public EnumFormaMesa Forma { get; set; } = EnumFormaMesa.Cuadrada;

        public EnumTamanioMesa Tamanio { get; set; } = EnumTamanioMesa.Mediana;

        public Sala? Sala { get; set; }
    }
}

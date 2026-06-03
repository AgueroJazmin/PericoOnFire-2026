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
    }
}

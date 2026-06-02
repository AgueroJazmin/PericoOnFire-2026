using  PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  PericoOnFire_2026.BD.Datos
{
    public class EntityBase : IEntityBase
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El campo es obligatorio.")]
        public EnumEstadoRegistro EstadoRegistro { get; set; } = EnumEstadoRegistro.EnGrabacion;
    }
}

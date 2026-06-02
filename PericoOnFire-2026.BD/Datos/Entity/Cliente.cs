using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  PericoOnFire_2026.BD.Datos.Entity
{
    public class Cliente : EntityBase
    {
        [Required(ErrorMessage = "El Nombre del cliente es oblgatorio")]
        [MaxLength(100, ErrorMessage = "El Nombre excede la cant")]
        public required string Nombre { get; set; }

        [Required(ErrorMessage = "La dirección del cliente es obligatoria")]
        [MaxLength(100, ErrorMessage = "La dirección excede la cantidad máxima de caracteres")]
        public required string Direccion { get; set; }

        [Required(ErrorMessage = "El numero de telefono del cliente es oblgatorio")]
        public required string Telefono { get; set; }
    }
}

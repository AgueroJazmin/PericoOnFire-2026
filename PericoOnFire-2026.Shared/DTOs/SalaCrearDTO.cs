using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class SalaCrearDTO
    {
        [Required(ErrorMessage = "El nombre de la sala es obligatorio")]
        public string Nombre { get; set; } = "";
        public int Orden { get; set; } = 0;
    }
}

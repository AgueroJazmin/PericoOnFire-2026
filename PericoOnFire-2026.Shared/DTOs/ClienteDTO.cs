using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class ClienteDTO
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = "";

        public string Direccion { get; set; } = ""; 

        public string Telefono { get; set; } = "";
    }
}

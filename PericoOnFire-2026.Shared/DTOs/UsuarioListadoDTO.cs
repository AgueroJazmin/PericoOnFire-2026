using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class UsuarioListadoDTO
    {
       
            public string Id { get; set; } = "";
            public string Email { get; set; } = "";
            public string UserName { get; set; } = "";
            public string Rol { get; set; } = "";
            public bool EmailConfirmed { get; set; }
       
    }
}

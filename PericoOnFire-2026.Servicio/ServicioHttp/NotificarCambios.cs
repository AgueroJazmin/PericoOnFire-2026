using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Servicio.ServicioHttp
{
    public class NotificarCambios
    {
        public event Action<string>? OnCambio;

        public void Notificacion(string entidad)
        {
            OnCambio?.Invoke(entidad);
        }
    }
}

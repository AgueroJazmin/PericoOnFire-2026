using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.ENUM
{
    public enum EnumEstadoRegistro
    {
        activo = 1,
        inactivo = 2,
        borrado = 3,
        EnGrabacion = 4// cuando algo fallo porque se estaba grabando 
    }
    public enum ResultadoOperacionSeguridad
    {
        Exitoso = 1,
        Fallido = 2,
        NoEncontrado = 3,
        SinPermiso = 4
    }
}

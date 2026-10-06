using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.Server.Servicios
{
    public static class CajaActual
    {
        //Hay como máximo un turno abierto a la vez: la caja del local. Tanto el cobro (PagoController)
        //como todo lo de caja (CajaController) tienen que apoyarse en el mismo turno abierto.
        public static Task<TurnoCaja?> ObtenerTurnoAbiertoAsync(MiDbContext context) =>
        context.TurnosCaja.FirstOrDefaultAsync(t =>
            t.Estado == EnumEstadoTurnoCaja.Abierto &&
            t.EstadoRegistro == EnumEstadoRegistro.activo);
    }
}

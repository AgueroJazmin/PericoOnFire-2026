using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;

namespace PericoOnFire_2026.Server.Servicios;

public static class NumeroComandaDiario
{
    private static DateTime FechaLocalArgentina(DateTime fechaUtc) =>
        DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc).AddHours(-3).Date;

    private static (DateTime InicioUtc, DateTime FinUtc) RangoUtc(DateTime fechaUtc)
    {
        var fechaLocal = FechaLocalArgentina(fechaUtc);
        var inicioUtc = DateTime.SpecifyKind(fechaLocal.AddHours(3), DateTimeKind.Utc);
        return (inicioUtc, inicioUtc.AddDays(1));
    }

    public static async Task<int> ObtenerAsync(
        MiDbContext context, int idComanda, DateTime fechaApertura)
    {
        var (inicioUtc, finUtc) = RangoUtc(fechaApertura);
        return await context.Comandas.CountAsync(c =>
            c.FechaApertura >= inicioUtc &&
            c.FechaApertura < finUtc &&
            c.Id <= idComanda);
    }

    public static async Task<Dictionary<int, int>> ObtenerVariosAsync(
        MiDbContext context,
        IEnumerable<(int Id, DateTime FechaApertura)> comandas)
    {
        var resultado = new Dictionary<int, int>();

        foreach (var grupo in comandas
            .DistinctBy(c => c.Id)
            .GroupBy(c => FechaLocalArgentina(c.FechaApertura)))
        {
            var inicioUtc = DateTime.SpecifyKind(grupo.Key.AddHours(3), DateTimeKind.Utc);
            var finUtc = inicioUtc.AddDays(1);
            var idsDelDia = await context.Comandas
                .Where(c => c.FechaApertura >= inicioUtc && c.FechaApertura < finUtc)
                .OrderBy(c => c.Id)
                .Select(c => c.Id)
                .ToListAsync();

            for (var indice = 0; indice < idsDelDia.Count; indice++)
                resultado[idsDelDia[indice]] = indice + 1;
        }

        return resultado;
    }
}

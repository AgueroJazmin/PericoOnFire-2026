using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace PericoOnFire_2026.BD.Datos;

public static class OperacionAtomica
{
    private const int MaximoIntentos = 5;

    public static bool EsConflictoConcurrente(Exception error)
    {
        for (Exception? actual = error; actual != null; actual = actual.InnerException)
            if (actual is PostgresException postgres && postgres.SqlState == "40001")
                return true;
        return false;
    }

    public static async Task<T> EjecutarAsync<T>(MiDbContext context, Func<Task<T>> accion)
    {
        var estrategia = context.Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async () =>
        {
            for (var intento = 1; ; intento++)
            {
                try
                {
                    // Cada intento consulta nuevamente los datos dentro de una transacción nueva.
                    context.ChangeTracker.Clear();
                    await using var tx = await context.Database.BeginTransactionAsync(
                        System.Data.IsolationLevel.Serializable);
                    var resultado = await accion();
                    await tx.CommitAsync();
                    return resultado;
                }
                catch (Exception ex) when (EsConflictoConcurrente(ex) && intento < MaximoIntentos)
                {
                    // La transacción fallida ya se descartó antes de esperar y reintentar.
                    context.ChangeTracker.Clear();
                    await Task.Delay(50 * intento + Random.Shared.Next(20, 80));
                }
            }
        });
    }
}

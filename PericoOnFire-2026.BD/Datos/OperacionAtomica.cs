using Microsoft.EntityFrameworkCore;
namespace PericoOnFire_2026.BD.Datos;
public static class OperacionAtomica
{
    public static async Task<T> EjecutarAsync<T>(MiDbContext context, Func<Task<T>> accion)
    {
        var estrategia = context.Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var tx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var resultado = await accion();
            await tx.CommitAsync();
            return resultado;
        });
    }
}

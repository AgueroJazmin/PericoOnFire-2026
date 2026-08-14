using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.SerPolling
{
    public class PollingService : IAsyncDisposable
    {
        private CancellationTokenSource? cts;
        private Task? loopTask;

        public void Iniciar(Func<Task> accion, TimeSpan intervalo)
        {
            Detener(); // por si ya había un polling corriendo en este componente

            cts = new CancellationTokenSource();
            var localTimer = new PeriodicTimer(intervalo);
            loopTask = EjecutarLoop(accion, localTimer, cts.Token);
        }

        private async Task EjecutarLoop(Func<Task> accion, PeriodicTimer localTimer, CancellationToken token)
        {
            try
            {
                while (await localTimer.WaitForNextTickAsync(token))
                {
                    await accion();
                }
            }
            catch (OperationCanceledException)
            {
                // esperado al llamar Detener() o al hacer Dispose
            }
            catch (ObjectDisposedException)
            {
                // el timer se dispuso mientras esperábamos el próximo tick, también esperado
            }
            finally
            {
                localTimer.Dispose();
            }
        }

        public void Detener()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        public async ValueTask DisposeAsync()
        {
            Detener();
            if (loopTask is not null)
            {
                try { await loopTask; } catch { /* ya cancelado, ignorar */ }
            }
        }


    }
}

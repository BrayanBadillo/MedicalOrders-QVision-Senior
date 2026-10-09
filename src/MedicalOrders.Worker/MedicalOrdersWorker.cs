using MediatR;
using MedicalOrders.Application.Orders.Commands.ProcessPendingOrders;
using MedicalOrders.Worker;
using Microsoft.Extensions.Options;

namespace MedicalOrders.MedicalOrdersWorker;


/// <summary>
/// Servicio en segundo plano: cada ciclo lee las órdenes pendientes y delega el procesamiento
/// al caso de uso <see cref="ProcessPendingOrdersCommand"/>. Un error en un ciclo no detiene el servicio.
/// </summary>
public sealed class MedicalOrdersWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerOptions> options,
    ILogger<MedicalOrdersWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var batchSize = Math.Max(1, settings.BatchSize);
        var interval = TimeSpan.FromSeconds(Math.Max(1, settings.PollingIntervalSeconds));

        logger.LogInformation(
            "Worker iniciado. Lote: {BatchSize} órdenes, intervalo: {Interval} s",
            batchSize, interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCycleAsync(batchSize, stoppingToken);

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Worker detenido");
    }

    private async Task RunCycleAsync(int batchSize, CancellationToken stoppingToken)
    {
        try
        {
            // El DbContext es scoped: se crea un scope nuevo por ciclo.
            using var scope = scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var result = await sender.Send(new ProcessPendingOrdersCommand(batchSize), stoppingToken);

            if (result.Total > 0)
            {
                logger.LogInformation(
                    "Ciclo completado: {Processed} procesadas, {Failed} fallidas, {Skipped} omitidas",
                    result.Processed, result.Failed, result.Skipped);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Apagado ordenado.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en el ciclo del Worker; se reintentará en el siguiente intervalo");
        }
    }
}

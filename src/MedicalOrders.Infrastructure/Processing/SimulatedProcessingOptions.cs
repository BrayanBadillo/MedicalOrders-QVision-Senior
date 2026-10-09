using MedicalOrders.Application.Abstractions;
using MedicalOrders.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalOrders.Infrastructure.Processing;

public sealed class SimulatedProcessingOptions
{
    public const string SectionName = "SimulatedProcessing";

    /// <summary>Duración simulada del procesamiento de cada orden.</summary>
    public int DelayMilliseconds { get; set; } = 2000;
}

/// <summary>Simula el envío de la orden al sistema destino. Reemplazable por una integración real.</summary>
public sealed class SimulatedOrderProcessor(
    IOptions<SimulatedProcessingOptions> options,
    ILogger<SimulatedOrderProcessor> logger) : IOrderProcessor
{
    public async Task ProcessAsync(Order order, CancellationToken cancellationToken)
    {
        var delay = Math.Max(0, options.Value.DelayMilliseconds);

        logger.LogInformation(
            "Procesando orden {OrderId} (servicio {ServiceCode}); duración simulada {DelayMs} ms",
            order.Id, order.ServiceCode, delay);

        await Task.Delay(delay, cancellationToken);
    }
}

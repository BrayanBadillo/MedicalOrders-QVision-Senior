using MediatR;
using MedicalOrders.Application.Abstractions;
using MedicalOrders.Application.Common.Exceptions;
using MedicalOrders.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MedicalOrders.Application.Orders.Commands.ProcessPendingOrders;

/// <summary>
/// Caso de uso del Worker: toma un lote de órdenes pendientes y las procesa una a una.
/// Cada orden se "reclama" primero (Pendiente → EnProceso) con control de concurrencia
/// optimista, de modo que dos instancias del Worker nunca procesan la misma orden.
/// </summary>
public sealed class ProcessPendingOrdersCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IOrderProcessor processor,
    TimeProvider clock,
    ILogger<ProcessPendingOrdersCommandHandler> logger) : IRequestHandler<ProcessPendingOrdersCommand, ProcessingBatchResult>
{
    private enum Outcome
    {
        Processed,
        Failed,
        Skipped
    }

    public async Task<ProcessingBatchResult> Handle(
        ProcessPendingOrdersCommand request,
        CancellationToken cancellationToken)
    {
        var pendingIds = await orders.GetPendingIdsAsync(request.BatchSize, cancellationToken);

        if (pendingIds.Count == 0)
            return new ProcessingBatchResult(0, 0, 0);

        logger.LogInformation("Worker: {Count} órdenes pendientes encontradas", pendingIds.Count);

        int processed = 0, failed = 0, skipped = 0;

        foreach (var id in pendingIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (await ProcessOrderAsync(id, cancellationToken))
            {
                case Outcome.Processed: processed++; break;
                case Outcome.Failed: failed++; break;
                default: skipped++; break;
            }
        }

        return new ProcessingBatchResult(processed, failed, skipped);
    }

    private async Task<Outcome> ProcessOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);
        if (order is null || order.Status != OrderStatus.Pendiente)
            return Outcome.Skipped;

        // 1) Reclamar la orden.
        try
        {
            order.MarkAsProcessing(Now());
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            unitOfWork.DiscardChanges();
            logger.LogInformation("Worker: la orden {OrderId} ya fue tomada por otro proceso; se omite", id);
            return Outcome.Skipped;
        }

        logger.LogInformation(
            "Worker: orden {OrderId} {From} → {To} (prioridad {Priority})",
            id, OrderStatus.Pendiente, OrderStatus.EnProceso, order.Priority);

        // 2) Procesarla.
        try
        {
            await processor.ProcessAsync(order, cancellationToken);
            order.MarkAsProcessed(Now());
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Worker: orden {OrderId} {From} → {To}",
                id, OrderStatus.EnProceso, OrderStatus.Procesada);

            return Outcome.Processed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Worker: error procesando la orden {OrderId}", id);
            await TryMarkAsFailedAsync(id, ex, cancellationToken);
            return Outcome.Failed;
        }
    }

    private async Task TryMarkAsFailedAsync(Guid id, Exception error, CancellationToken cancellationToken)
    {
        try
        {
            // Se parte de un estado limpio: el intento fallido pudo dejar cambios sin guardar.
            unitOfWork.DiscardChanges();

            var order = await orders.GetByIdAsync(id, cancellationToken);
            if (order is null || order.Status != OrderStatus.EnProceso)
                return;

            order.MarkAsFailed(error.Message, Now());
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogWarning(
                "Worker: orden {OrderId} {From} → {To}",
                id, OrderStatus.EnProceso, OrderStatus.Fallida);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Worker: no fue posible marcar la orden {OrderId} como Fallida", id);
        }
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
}

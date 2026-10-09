using MediatR;

namespace MedicalOrders.Application.Orders.Commands.ProcessPendingOrders;

public sealed record ProcessPendingOrdersCommand(int BatchSize) : IRequest<ProcessingBatchResult>;

public sealed record ProcessingBatchResult(int Processed, int Failed, int Skipped)
{
    public int Total => Processed + Failed + Skipped;
}


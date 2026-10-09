using MedicalOrders.Domain.Entities;

namespace MedicalOrders.Application.Abstractions;

public interface IOrderProcessor
{
    Task ProcessAsync(Order order, CancellationToken cancellationToken);
}

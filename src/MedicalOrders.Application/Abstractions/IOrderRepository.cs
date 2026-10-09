using MedicalOrders.Domain.Entities;
using MedicalOrders.Domain.Enums;

namespace MedicalOrders.Application.Abstractions;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);

    /// <summary>Obtiene la orden con seguimiento de cambios (para modificarla).</summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Obtiene la orden sin seguimiento de cambios (solo lectura).</summary>
    Task<Order?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        string? patientId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Ids de órdenes pendientes: primero las urgentes y luego las más antiguas.</summary>
    Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batchSize, CancellationToken cancellationToken);
}

using MedicalOrders.Application.Abstractions;
using MedicalOrders.Domain.Entities;
using MedicalOrders.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedicalOrders.Infrastructure.Persistence;

public sealed class OrderRepository(OrderDbContext db) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken cancellationToken) =>
        await db.Orders.AddAsync(order, cancellationToken);

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Order?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken) =>
        db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        string? patientId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(patientId))
            query = query.Where(o => o.PatientId == patientId);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .ThenBy(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batchSize, CancellationToken cancellationToken) =>
        await db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Pendiente)
            .OrderBy(o => o.Priority == Priority.Urgente ? 0 : 1)
            .ThenBy(o => o.CreatedAt)
            .Select(o => o.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
}

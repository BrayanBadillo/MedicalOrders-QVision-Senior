using MedicalOrders.Application.Abstractions;
using MedicalOrders.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MedicalOrders.Infrastructure.Persistence;

public sealed class UnitOfWork(OrderDbContext db) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("La orden fue modificada por otro proceso.", ex);
        }
    }

    public void DiscardChanges() => db.ChangeTracker.Clear();
}

namespace MedicalOrders.Application.Abstractions;

public interface IUnitOfWork
{
    /// <exception cref="Common.Exceptions.ConcurrencyConflictException">
    /// Otro proceso modificó la misma orden desde que fue leída.
    /// </exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Descarta los cambios pendientes y las entidades en seguimiento.</summary>
    void DiscardChanges();
}
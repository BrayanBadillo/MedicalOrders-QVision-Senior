using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalOrders.Infrastructure.Persistence;
public static class DatabaseInitializer
{
    /// <summary>Aplica las migraciones pendientes y activa el modo WAL de SQLite.</summary>
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        // WAL permite que la API escriba mientras el Worker lee/escribe sobre el mismo archivo.
        // El modo queda guardado en el propio archivo de la base de datos.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
    }
}
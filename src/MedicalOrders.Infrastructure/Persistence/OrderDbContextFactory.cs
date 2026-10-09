using MedicalOrders.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MedicalOrders.Infrastructure.Persistence;

public sealed class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlite(SolutionPaths.ResolveConnectionString(SolutionPaths.DefaultConnectionString))
            .Options;

        return new OrderDbContext(options);
    }
}

using MedicalOrders.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalOrders.Infrastructure.Persistence;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);
}


using MedicalOrders.Application.Abstractions;
using MedicalOrders.Infrastructure.Common;
using MedicalOrders.Infrastructure.Persistence;
using MedicalOrders.Infrastructure.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalOrders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = SolutionPaths.ResolveConnectionString(
            configuration.GetConnectionString("Default") ?? SolutionPaths.DefaultConnectionString);

        services.AddDbContext<OrderDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<SimulatedProcessingOptions>(
            configuration.GetSection(SimulatedProcessingOptions.SectionName));
        services.AddScoped<IOrderProcessor, SimulatedOrderProcessor>();

        return services;
    }
}


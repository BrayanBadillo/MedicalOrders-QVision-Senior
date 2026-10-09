using MedicalOrders.Application;
using MedicalOrders.Infrastructure;
using MedicalOrders.Infrastructure.Logging;
using MedicalOrders.MedicalOrdersWorker;
using MedicalOrders.Worker;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .UseSerilog((context, _, logger) => logger.ConfigureOrderLogging(context.Configuration, "worker"))
    .ConfigureServices(static (context, services) =>
    {
        services.AddApplication();
        services.AddInfrastructure(context.Configuration);
        services.Configure<WorkerOptions>(context.Configuration.GetSection(WorkerOptions.SectionName));
        services.AddHostedService<MedicalOrdersWorker>();
    })
    .Build();

await host.RunAsync();

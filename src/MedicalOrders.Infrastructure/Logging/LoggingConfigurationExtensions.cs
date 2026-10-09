using MedicalOrders.Infrastructure.Common;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace MedicalOrders.Infrastructure.Logging;

public static class LoggingConfigurationExtensions
{
    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{Application}] [{CorrelationId}] {SourceContext} - {Message:lj}{NewLine}{Exception}";

    /// <summary>Consola + archivo con rotación diaria (logs/{appName}-YYYYMMDD.log).</summary>
    public static LoggerConfiguration ConfigureOrderLogging(
        this LoggerConfiguration configuration,
        IConfiguration appConfiguration,
        string appName)
    {
        var logsDirectory = SolutionPaths.ResolveLogsDirectory(appConfiguration);
        Directory.CreateDirectory(logsDirectory);

        return configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .ReadFrom.Configuration(appConfiguration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", appName)
            .WriteTo.Console(outputTemplate: OutputTemplate)
            .WriteTo.File(
                Path.Combine(logsDirectory, $"{appName}-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: OutputTemplate);
    }
}

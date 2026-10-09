namespace MedicalOrders.Worker;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>Máximo de órdenes que se toman en cada ciclo.</summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>Segundos entre ciclos de consulta de órdenes pendientes.</summary>
    public int PollingIntervalSeconds { get; set; } = 5;
}

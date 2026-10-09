using MedicalOrders.Domain.Enums;
using MedicalOrders.Domain.Exceptions;

namespace MedicalOrders.Domain.Entities;

public sealed class Order
{

    public const int MaxFailureReasonLength = 1000;

    // Requerido por EF Core.
    private Order()
    {
    }

    public Guid Id { get; private set; }
    public string PatientId { get; private set; } = string.Empty;
    public string PatientName { get; private set; } = string.Empty;
    public string ServiceCode { get; private set; } = string.Empty;
    public string ServiceDescription { get; private set; } = string.Empty;
    public Priority Priority { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessingStartedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public static Order Create(
        string patientId,
        string? patientName,
        string serviceCode,
        string? serviceDescription,
        Priority priority,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            throw new DomainException("PatientId es obligatorio.");

        if (string.IsNullOrWhiteSpace(serviceCode))
            throw new DomainException("ServiceCode es obligatorio.");

        if (!Enum.IsDefined(priority))
            throw new DomainException($"Priority '{priority}' no es válida. Valores permitidos: Normal, Urgente.");

        return new Order
        {
            Id = Guid.NewGuid(),
            PatientId = patientId.Trim(),
            PatientName = patientName?.Trim() ?? string.Empty,
            ServiceCode = serviceCode.Trim(),
            ServiceDescription = serviceDescription?.Trim() ?? string.Empty,
            Priority = priority,
            Status = OrderStatus.Pendiente,
            CreatedAt = utcNow
        };
    }

    public void MarkAsProcessing(DateTime utcNow)
    {
        EnsureCurrentStatus(OrderStatus.Pendiente, OrderStatus.EnProceso);
        Status = OrderStatus.EnProceso;
        ProcessingStartedAt = utcNow;
    }

    public void MarkAsProcessed(DateTime utcNow)
    {
        EnsureCurrentStatus(OrderStatus.EnProceso, OrderStatus.Procesada);
        Status = OrderStatus.Procesada;
        ProcessedAt = utcNow;
        FailureReason = null;
    }

    public void MarkAsFailed(string reason, DateTime utcNow)
    {
        EnsureCurrentStatus(OrderStatus.EnProceso, OrderStatus.Fallida);
        Status = OrderStatus.Fallida;
        ProcessedAt = utcNow;
        FailureReason = reason.Length > MaxFailureReasonLength ? reason[..MaxFailureReasonLength] : reason;
    }

    private void EnsureCurrentStatus(OrderStatus expected, OrderStatus target)
    {
        if (Status != expected)
            throw new DomainException($"Transición inválida para la orden {Id}: de {Status} a {target}. Se esperaba el estado {expected}.");
    }
}

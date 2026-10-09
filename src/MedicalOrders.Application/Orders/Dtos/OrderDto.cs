using MedicalOrders.Domain.Entities;

namespace MedicalOrders.Application.Orders.Dtos;

public sealed record OrderDto(
    Guid Id,
    string PatientId,
    string PatientName,
    string ServiceCode,
    string ServiceDescription,
    string Priority,
    string Status,
    DateTime CreatedAt,
    DateTime? ProcessingStartedAt,
    DateTime? ProcessedAt,
    string? FailureReason);

public static class OrderMappings
{
    public static OrderDto ToDto(this Order order) => new(
        order.Id,
        order.PatientId,
        order.PatientName,
        order.ServiceCode,
        order.ServiceDescription,
        order.Priority.ToString(),
        order.Status.ToString(),
        order.CreatedAt,
        order.ProcessingStartedAt,
        order.ProcessedAt,
        order.FailureReason);
}

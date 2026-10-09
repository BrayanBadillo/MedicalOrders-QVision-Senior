namespace MedicalOrders.Api.Contracts;

public sealed record CreateOrderRequest(
    string? PatientId,
    string? PatientName,
    string? ServiceCode,
    string? ServiceDescription,
    string? Priority);

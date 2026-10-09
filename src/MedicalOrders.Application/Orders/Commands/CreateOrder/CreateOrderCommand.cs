using MediatR;
using MedicalOrders.Application.Orders.Dtos;

namespace MedicalOrders.Application.Orders.Commands.CreateOrder;

public sealed record CreateOrderCommand(
    string PatientId,
    string PatientName,
    string ServiceCode,
    string ServiceDescription,
    string Priority) : IRequest<OrderDto>;

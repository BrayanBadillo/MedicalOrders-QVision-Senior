using MediatR;
using MedicalOrders.Application.Orders.Dtos;

namespace MedicalOrders.Application.Orders.Queries.GetOrder;

public sealed record GetOrderQuery(Guid Id) : IRequest<OrderDto>;

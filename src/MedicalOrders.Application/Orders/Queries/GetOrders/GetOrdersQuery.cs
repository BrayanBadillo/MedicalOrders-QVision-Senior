using MediatR;
using MedicalOrders.Application.Common;
using MedicalOrders.Application.Orders.Dtos;

namespace MedicalOrders.Application.Orders.Queries.GetOrders;

public sealed record GetOrdersQuery(
    string? PatientId,
    string? Status,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<OrderDto>>;

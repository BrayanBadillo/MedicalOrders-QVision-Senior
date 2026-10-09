using MediatR;
using MedicalOrders.Application.Abstractions;
using MedicalOrders.Application.Common;
using MedicalOrders.Application.Orders.Dtos;
using MedicalOrders.Domain.Enums;

namespace MedicalOrders.Application.Orders.Queries.GetOrders;

public sealed class GetOrdersQueryHandler(IOrderRepository orders)
    : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        OrderStatus? status = EnumParser.TryParse<OrderStatus>(request.Status, out var parsed) ? parsed : null;
        var patientId = string.IsNullOrWhiteSpace(request.PatientId) ? null : request.PatientId.Trim();

        var (items, total) = await orders.ListAsync(
            patientId, status, request.Page, request.PageSize, cancellationToken);

        return new PagedResult<OrderDto>(
            items.Select(o => o.ToDto()).ToList(),
            request.Page,
            request.PageSize,
            total);
    }
}

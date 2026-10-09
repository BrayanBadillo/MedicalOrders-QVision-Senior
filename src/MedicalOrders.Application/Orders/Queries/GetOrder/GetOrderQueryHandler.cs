using MediatR;
using MedicalOrders.Application.Abstractions;
using MedicalOrders.Application.Common.Exceptions;
using MedicalOrders.Application.Orders.Dtos;
using MedicalOrders.Domain.Entities;

namespace MedicalOrders.Application.Orders.Queries.GetOrder;

public sealed class GetOrderQueryHandler(IOrderRepository orders) : IRequestHandler<GetOrderQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdReadOnlyAsync(request.Id, cancellationToken)
                    ?? throw new NotFoundException(nameof(Order), request.Id);

        return order.ToDto();
    }
}

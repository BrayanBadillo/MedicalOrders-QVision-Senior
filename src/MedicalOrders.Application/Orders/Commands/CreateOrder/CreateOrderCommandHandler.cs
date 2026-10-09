using MediatR;
using MedicalOrders.Application.Abstractions;
using MedicalOrders.Application.Common;
using MedicalOrders.Application.Orders.Dtos;
using MedicalOrders.Domain.Entities;
using MedicalOrders.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MedicalOrders.Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // El validador ya garantizó que la prioridad es válida.
        EnumParser.TryParse<Priority>(request.Priority, out var priority);

        var order = Order.Create(
            request.PatientId,
            request.PatientName,
            request.ServiceCode,
            request.ServiceDescription,
            priority,
            clock.GetUtcNow().UtcDateTime);

        await orders.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Se registra el PatientId (identificador) pero nunca el nombre del paciente.
        logger.LogInformation(
            "Orden creada {OrderId}: paciente {PatientId}, servicio {ServiceCode}, prioridad {Priority}, estado {Status}",
            order.Id, order.PatientId, order.ServiceCode, order.Priority, order.Status);

        return order.ToDto();
    }
}

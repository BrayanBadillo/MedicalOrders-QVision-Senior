using MediatR;
using MedicalOrders.Api.Contracts;
using MedicalOrders.Application.Common;
using MedicalOrders.Application.Orders.Commands.CreateOrder;
using MedicalOrders.Application.Orders.Dtos;
using MedicalOrders.Application.Orders.Queries.GetOrder;
using MedicalOrders.Application.Orders.Queries.GetOrders;
using Microsoft.AspNetCore.Mvc;

namespace MedicalOrders.Api.Controller;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    /// <summary>Registra una orden médica. Queda en estado Pendiente hasta que el Worker la procese.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.PatientId ?? string.Empty,
            request.PatientName ?? string.Empty,
            request.ServiceCode ?? string.Empty,
            request.ServiceDescription ?? string.Empty,
            request.Priority ?? string.Empty);

        var order = await sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>Lista órdenes con filtros opcionales y paginación.</summary>
    /// <param name="patientId">Filtra por identificador de paciente.</param>
    /// <param name="status">Pendiente, EnProceso, Procesada o Fallida.</param>
    /// <param name="page">Número de página (desde 1).</param>
    /// <param name="pageSize">Tamaño de página (1 a 100).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetAll(
        [FromQuery] string? patientId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetOrdersQuery(patientId, status, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Consulta el detalle de una orden.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await sender.Send(new GetOrderQuery(id), cancellationToken);
        return Ok(order);
    }
}

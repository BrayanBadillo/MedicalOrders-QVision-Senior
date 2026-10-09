using MedicalOrders.Application.Common.Exceptions;
using MedicalOrders.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace MedicalOrders.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation(
                "Solicitud cancelada por el cliente: {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 499;
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var problem = ToProblemDetails(exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Error no controlado en {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning(
                "Solicitud rechazada ({Status}) en {Method} {Path}: {Detail}",
                problem.Status, context.Request.Method, context.Request.Path, problem.Detail);
        }

        if (context.Response.HasStarted)
            return;

        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        if (context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
            problem.Extensions["correlationId"] = correlationId;

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }

    private static ProblemDetails ToProblemDetails(Exception exception)
    {
        switch (exception)
        {
            case FluentValidation.ValidationException validation:
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Error de validación",
                    Detail = "Uno o más campos no son válidos."
                };
                problem.Extensions["errors"] = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return problem;

            case DomainException domain:
                return new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Regla de negocio incumplida",
                    Detail = domain.Message
                };

            case NotFoundException notFound:
                return new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Recurso no encontrado",
                    Detail = notFound.Message
                };

            case ConcurrencyConflictException conflict:
                return new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflicto de concurrencia",
                    Detail = conflict.Message
                };

            default:
                // No se expone el detalle interno al cliente.
                return new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Error interno del servidor",
                    Detail = "Ocurrió un error inesperado. Use el traceId para rastrearlo en los logs."
                };
        }
    }
}

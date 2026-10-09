using FluentValidation;
using MedicalOrders.Application.Common;
using MedicalOrders.Domain.Enums;

namespace MedicalOrders.Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("PatientId es obligatorio.")
            .MaximumLength(50).WithMessage("PatientId no puede superar los 50 caracteres.");

        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithMessage("ServiceCode es obligatorio.")
            .MaximumLength(50).WithMessage("ServiceCode no puede superar los 50 caracteres.");

        RuleFor(x => x.PatientName)
            .MaximumLength(200).WithMessage("PatientName no puede superar los 200 caracteres.");

        RuleFor(x => x.ServiceDescription)
            .MaximumLength(500).WithMessage("ServiceDescription no puede superar los 500 caracteres.");

        RuleFor(x => x.Priority)
            .Must(value => EnumParser.TryParse<Priority>(value, out _))
            .WithMessage("Priority debe ser 'Normal' o 'Urgente'.");
    }
}
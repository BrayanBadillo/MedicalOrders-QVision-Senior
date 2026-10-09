namespace MedicalOrders.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource, object key)
        : base($"{resource} con id '{key}' no existe.")
    {
    }
}

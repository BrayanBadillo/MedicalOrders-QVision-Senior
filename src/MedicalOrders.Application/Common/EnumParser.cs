namespace MedicalOrders.Application.Common;

public static class EnumParser
{
    /// <summary>
    /// Convierte texto a enum por nombre (sin distinguir mayúsculas). Rechaza números,
    /// listas separadas por coma y valores no definidos.
    /// </summary>
    public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var text = value.Trim();

        if (char.IsDigit(text[0]) || text[0] == '-' || text.Contains(','))
            return false;

        return Enum.TryParse(text, ignoreCase: true, out result) && Enum.IsDefined(result);
    }
}

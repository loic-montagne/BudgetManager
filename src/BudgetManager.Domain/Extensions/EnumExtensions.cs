namespace BudgetManager.Domain.Extensions;

public static class EnumExtensions
{
    public static bool IsValid<TEnum>(this TEnum value)
        where TEnum : struct, Enum
    {
        return Enum.IsDefined(value);
    }

    public static void ThrowIfNotValid<TEnum>(this TEnum value, string? paramName = null)
        where TEnum : struct, Enum
    {
        if (!value.IsValid())
            throw new ArgumentException("Invalid enum value.", paramName);
    }

}

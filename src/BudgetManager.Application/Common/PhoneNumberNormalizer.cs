namespace BudgetManager.Application.Common;

public static class PhoneNumberNormalizer
{
    public static string? Normalize(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        var normalized =
            phoneNumber
                .Replace(" ", string.Empty)
                .Replace(".", string.Empty)
                .Replace("-", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty);

        if (normalized.StartsWith("00"))
            return $"+{normalized[2..]}";

        if (normalized.StartsWith('0'))
            return $"+33{normalized[1..]}";

        return normalized;
    }
}

using System.Text.RegularExpressions;

namespace BudgetManager.Domain.ValueObjects;

public static partial class IbanValidator
{
    [GeneratedRegex("^[A-Z0-9]+$")]
    public static partial Regex Alphanumeric();
}
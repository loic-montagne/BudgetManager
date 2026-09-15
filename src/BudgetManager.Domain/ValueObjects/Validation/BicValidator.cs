using System.Text.RegularExpressions;

namespace BudgetManager.Domain.ValueObjects;

public static partial class BicValidator
{
    [GeneratedRegex("^[A-Z0-9]+$")]
    public static partial Regex Alphanumeric();

    [GeneratedRegex("^[A-Z]{4}")]
    public static partial Regex BankCode();
}
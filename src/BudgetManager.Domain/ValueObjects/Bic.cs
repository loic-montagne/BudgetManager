using BudgetManager.Domain.Common;
using BudgetManager.Domain.Exceptions;

namespace BudgetManager.Domain.ValueObjects;

public sealed record Bic
{
    public string Value { get; private set; }

    private Bic()
    {
        Value = string.Empty;
    }
    private Bic(string value)
    {
        Value = value;
    }

    public static Bic Create(string bic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bic);

        var normalizedBic = bic.Replace(" ", string.Empty)
                               .Replace("\u00A0", string.Empty)
                               .Trim()
                               .ToUpperInvariant();

        if (!BicValidator.Alphanumeric().IsMatch(normalizedBic))
            throw new InvalidBicException("BIC contains invalid characters.", ErrorCodes.BicInvalidChars);

        if (normalizedBic.Length != 8 && normalizedBic.Length != 11)
            throw new InvalidBicException("BIC must have 8 or 11 characters.", ErrorCodes.BicInvalidCharsCount);

        if (!BicValidator.BankCode().IsMatch(normalizedBic))
            throw new InvalidBicException("BIC bank code must contain only letters.", ErrorCodes.BicBankCodeInvalidChars);

        if (normalizedBic[4..6] != "FR")
            throw new InvalidBicException("Only french BICs are supported.", ErrorCodes.BicFrenchOnly);

        return new Bic(normalizedBic);
    }

    public override string ToString()
    {
        return Value;
    }
}

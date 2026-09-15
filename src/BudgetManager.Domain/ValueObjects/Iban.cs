using BudgetManager.Domain.Common;
using BudgetManager.Domain.Exceptions;

namespace BudgetManager.Domain.ValueObjects;

public sealed record Iban
{
    public string Value { get; private set; }

    private Iban() 
    {
        Value = string.Empty;
    }
    private Iban(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public string ToDisplayString()
    {
        return Format(Value);
    }

    public static Iban Create(string iban)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iban);

        var normalizedIban = iban.Replace(" ", string.Empty)
                                 .Replace("\u00A0", string.Empty)
                                 .Trim()
                                 .ToUpperInvariant();

        if (!IbanValidator.Alphanumeric().IsMatch(normalizedIban))
            throw new InvalidIbanException("IBAN contains invalid characters.", ErrorCodes.IbanInvalidChars);

        if (!normalizedIban.StartsWith("FR"))
            throw new InvalidIbanException("Only French IBANs are supported.", ErrorCodes.IbanFrenchOnly);

        if (normalizedIban.Length != 27)
            throw new InvalidIbanException("IBAN must have exactly 27 characters.", ErrorCodes.IbanInvalidCharsCount);

        if (!IsChecksumValid(normalizedIban))
            throw new InvalidIbanException("IBAN checksum is invalid.", ErrorCodes.IbanInvalidChecksum);

        return new Iban(normalizedIban);
    }

    private static bool IsChecksumValid(string iban)
    {
        var rearranged = iban[4..] + iban[..4];
        var remainder = 0;
        foreach (var c in rearranged)
        {
            if (c is >= 'A' and <= 'Z')
            {
                var value = c - 'A' + 10;
                foreach (var digit in value.ToString())
                {
                    remainder = (remainder * 10 + (digit - '0')) % 97;
                }
            }
            else
            {
                remainder = (remainder * 10 + (c - '0')) % 97;
            }
        }
        return remainder == 1;
    }
    private static string Format(string iban)
    {
        return string.Join(" ", Enumerable.Range(0, (iban.Length + 3) / 4)
                                          .Select(i => iban.Substring(i * 4,
                                                                      Math.Min(4, iban.Length - i * 4))));
    }
}

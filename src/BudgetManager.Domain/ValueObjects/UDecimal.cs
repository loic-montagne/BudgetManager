using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BudgetManager.Domain.ValueObjects;

public readonly struct UDecimal :
    IEquatable<UDecimal>,
    IParsable<UDecimal>,
    ISpanParsable<UDecimal>,
    IFormattable,
    ISpanFormattable
{
    /// <summary>
    /// Represents the smallest possible value of <see cref="UDecimal"/> (0).
    /// </summary>
    public static readonly UDecimal MinValue = 0m;

    /// <summary>
    /// Represents the largest possible value of <see cref="UDecimal"/> (equivalent to <see cref="decimal.MaxValue"/>).
    /// </summary>
    public static readonly UDecimal MaxValue = decimal.MaxValue;

    /// <summary>
    /// Equivalent to <see cref="decimal.One"/>.
    /// </summary>
    public static readonly UDecimal One = decimal.One;

    /// <summary>
    /// Equivalent to <see cref="decimal.Zero"/>.
    /// </summary>
    public static readonly UDecimal Zero = decimal.Zero;

    private readonly decimal value;

    public UDecimal(decimal value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        this.value = value;
    }

    public static implicit operator decimal(UDecimal d)
        => d.value;
    public static implicit operator UDecimal(decimal d)
        => new(d);
    public static bool operator <(UDecimal a, UDecimal b)
        => a.value < b.value;
    public static bool operator >(UDecimal a, UDecimal b)
        => a.value > b.value;
    public static bool operator ==(UDecimal a, UDecimal b)
        => a.value == b.value;
    public static bool operator !=(UDecimal a, UDecimal b)
        => a.value != b.value;
    public static bool operator <=(UDecimal a, UDecimal b)
        => a.value <= b.value;
    public static bool operator >=(UDecimal a, UDecimal b)
        => a.value >= b.value;
    public static UDecimal operator +(UDecimal a, UDecimal b)
        => new(a.value + b.value);
    public static UDecimal operator -(UDecimal a, UDecimal b)
        => new(a.value - b.value);
    public static UDecimal operator *(UDecimal a, UDecimal b)
        => new(a.value * b.value);
    public static UDecimal operator /(UDecimal a, UDecimal b)
        => new(a.value / b.value);

    public readonly bool Equals(UDecimal a)
        => value == a.value;
    public override readonly bool Equals(object? a)
        => a is UDecimal other && Equals(other);

    public override int GetHashCode()
        => value.GetHashCode();

    private static string? PrepareParsedValue(string? s, IFormatProvider? provider)
        => s?.Replace(".", (provider as CultureInfo ?? CultureInfo.CurrentCulture).NumberFormat.NumberDecimalSeparator);
    private static ReadOnlySpan<char> PrepareParsedValue(ReadOnlySpan<char> s, IFormatProvider? provider)
        => new((PrepareParsedValue(s.ToString(), provider) ?? string.Empty).ToArray());

    public static UDecimal Parse(string s, IFormatProvider? provider)
        => new(decimal.Parse(PrepareParsedValue(s, provider) ?? string.Empty, provider));
    public static bool TryParse(string? s, IFormatProvider? provider, out UDecimal result)
    {
        if (decimal.TryParse(PrepareParsedValue(s, provider), NumberStyles.Number, provider, out var d) && d >= 0)
        {
            result = new UDecimal(d);
            return true;
        }
        result = default;
        return false;
    }

    public static UDecimal Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        => new(decimal.Parse(PrepareParsedValue(s, provider), provider));
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out UDecimal result)
    {
        if (decimal.TryParse(PrepareParsedValue(s, provider), NumberStyles.Number, provider, out var d) && d >= 0)
        {
            result = new UDecimal(d);
            return true;
        }
        result = default;
        return false;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        => value.TryFormat(destination, out charsWritten, format, provider);

    public decimal ToDecimal()
        => value;

    public override string ToString()
        => value.ToString(CultureInfo.CurrentCulture);

    public string ToString(IFormatProvider? provider)
        => value.ToString(provider);

    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        => value.ToString(format, CultureInfo.CurrentCulture);

    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        => value.ToString(format, provider);

}

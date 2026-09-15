using BudgetManager.Domain.Exceptions;
using BudgetManager.Domain.ValueObjects;
using Xunit;

namespace BudgetManager.Domain.Tests.ValueObjects;

public sealed class IbanTests
{
    [Theory]
    [InlineData("FR76 3000 6000 0112 3456 7890 189", "FR7630006000011234567890189")]
    [InlineData("fr7630006000011234567890189", "FR7630006000011234567890189")]
    public void Create_WithValidFrenchIban_NormalizesValue(string input, string expected)
    {
        var iban = Iban.Create(input);

        Assert.Equal(expected, iban.Value);
        Assert.Equal("FR76 3000 6000 0112 3456 7890 189", iban.ToDisplayString());
    }

    [Theory]
    [InlineData("DE89370400440532013000")]
    [InlineData("FR7630006000011234567890188")]
    [InlineData("FR76-30006000011234567890189")]
    public void Create_WithInvalidIban_Throws(string input)
    {
        Assert.Throws<InvalidIbanException>(() => Iban.Create(input));
    }
}

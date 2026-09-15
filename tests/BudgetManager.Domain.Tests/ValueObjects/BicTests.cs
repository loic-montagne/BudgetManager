using BudgetManager.Domain.Exceptions;
using BudgetManager.Domain.ValueObjects;
using Xunit;

namespace BudgetManager.Domain.Tests.ValueObjects;

public sealed class BicTests
{
    [Theory]
    [InlineData("BNPAFRPP", "BNPAFRPP")]
    [InlineData("bnpa fr pp xxx", "BNPAFRPPXXX")]
    public void Create_WithValidFrenchBic_NormalizesValue(string input, string expected)
    {
        var bic = Bic.Create(input);

        Assert.Equal(expected, bic.Value);
        Assert.Equal(expected, bic.ToString());
    }

    [Theory]
    [InlineData("DEUTDEFF")]
    [InlineData("B1PAFRPP")]
    [InlineData("BNPAFRP")]
    [InlineData("BNPAFRPP!")]
    public void Create_WithInvalidBic_Throws(string input)
    {
        Assert.Throws<InvalidBicException>(() => Bic.Create(input));
    }
}

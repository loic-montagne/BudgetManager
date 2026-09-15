using BudgetManager.Domain.Exceptions;
using BudgetManager.Domain.ValueObjects;
using Xunit;

namespace BudgetManager.Domain.Tests.ValueObjects;

public sealed class ValueObjectBranchCoverageTests
{
    [Theory]
    [InlineData("FR76-30006000011234567890189", BudgetManager.Domain.Common.ErrorCodes.IbanInvalidChars)]
    [InlineData("DE89370400440532013000", BudgetManager.Domain.Common.ErrorCodes.IbanFrenchOnly)]
    [InlineData("FR761234", BudgetManager.Domain.Common.ErrorCodes.IbanInvalidCharsCount)]
    [InlineData("FR7630006000011234567890188", BudgetManager.Domain.Common.ErrorCodes.IbanInvalidChecksum)]
    public void Iban_Create_ForEachInvalidInputClass_ReturnsExpectedErrorCode(
        string value,
        string expectedErrorCode)
    {
        var exception = Assert.Throws<InvalidIbanException>(
            () => Iban.Create(value));

        Assert.Equal(expectedErrorCode, exception.ErrorCode);
    }

    [Theory]
    [InlineData("BNPAFRP!", BudgetManager.Domain.Common.ErrorCodes.BicInvalidChars)]
    [InlineData("BNPAFRP", BudgetManager.Domain.Common.ErrorCodes.BicInvalidCharsCount)]
    [InlineData("B1PAFRPP", BudgetManager.Domain.Common.ErrorCodes.BicBankCodeInvalidChars)]
    [InlineData("BNPADEPP", BudgetManager.Domain.Common.ErrorCodes.BicFrenchOnly)]
    public void Bic_Create_ForEachInvalidInputClass_ReturnsExpectedErrorCode(
        string value,
        string expectedErrorCode)
    {
        var exception = Assert.Throws<InvalidBicException>(
            () => Bic.Create(value));

        Assert.Equal(expectedErrorCode, exception.ErrorCode);
    }

    [Fact]
    public void UDecimal_MaxValue_CanBeCreatedAndConverted()
    {
        UDecimal value = decimal.MaxValue;

        Assert.Equal(decimal.MaxValue, value.ToDecimal());
    }

    [Fact]
    public void UDecimal_Addition_WhenDecimalWouldOverflow_ThrowsOverflowException()
    {
        Action action = () => _ = UDecimal.MaxValue + UDecimal.One;

        Assert.Throws<OverflowException>(action);
    }

    [Fact]
    public void UDecimal_Multiplication_WhenDecimalWouldOverflow_ThrowsOverflowException()
    {
        UDecimal multiplier = 2m;

        Action action = () => _ = UDecimal.MaxValue * multiplier;

        Assert.Throws<OverflowException>(action);
    }

    [Fact]
    public void UDecimal_Subtraction_WhenValuesAreEqual_ReturnsZero()
    {
        UDecimal left = 42m;
        UDecimal right = 42m;

        var result = left - right;

        Assert.Equal(UDecimal.Zero, result);
    }
}

using BudgetManager.Domain.Common;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.ValueObjects;
using Xunit;

namespace BudgetManager.Domain.Tests.Entities;

public sealed class BankTests
{
    [Fact]
    public void Bank_CanBeRenamedAndChangeBic()
    {
        var bank = Bank.Create(" Banque ", Bic.Create("BNPAFRPP"));
        var newBic = Bic.Create("AGRIFRPP");

        bank.Rename("Nouvelle banque");
        bank.ChangeBic(newBic);

        Assert.Equal("Nouvelle banque", bank.Name);
        Assert.Equal(newBic, bank.Bic);
    }

    [Fact]
    public void Create_WhenNameHasMaximumLength_CreatesBank()
    {
        // Arrange

        var name = new string(
            'B',
            StringPropertyLengths.NameLength);

        var bic = Bic.Create(
            "BNPAFRPP");

        // Act

        var bank = Bank.Create(
            name,
            bic);

        // Assert

        Assert.Equal(
            name,
            bank.Name);
    }

    [Fact]
    public void Create_WhenNameExceedsMaximumLength_Throws()
    {
        // Arrange

        var name = new string(
            'B',
            StringPropertyLengths.NameLength + 1);

        var bic = Bic.Create(
            "BNPAFRPP");

        // Act

        var action = () =>
            Bank.Create(
                name,
                bic);

        // Assert

        var exception =
            Assert.Throws<ArgumentException>(
                action);

        Assert.Equal(
            "name",
            exception.ParamName);
    }

    [Fact]
    public void Rename_WhenNameExceedsMaximumLength_Throws()
    {
        // Arrange

        var bank = Bank.Create(
            "Banque",
            Bic.Create("BNPAFRPP"));

        var name = new string(
            'B',
            StringPropertyLengths.NameLength + 1);

        // Act

        var action = () =>
            bank.Rename(name);

        // Assert

        Assert.Throws<ArgumentException>(
            action);
    }
}

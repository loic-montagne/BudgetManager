using BudgetManager.Domain.Common;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Exceptions;
using BudgetManager.Domain.ValueObjects;
using Xunit;

namespace BudgetManager.Domain.Tests.Entities;

public sealed class AccountTests
{
    private static readonly Iban ValidIban = Iban.Create("FR7630006000011234567890189");

    [Fact]
    public void Create_WithValidValues_TrimsNameAndInitializesOpenAccount()
    {
        var bankId = Guid.NewGuid();

        var account = Account.Create("  Compte courant  ", bankId, ValidIban);

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal("Compte courant", account.Name);
        Assert.Equal(bankId, account.BankId);
        Assert.False(account.IsClosed);
    }

    [Fact]
    public void ClosedAccount_CannotBeRenamedOrClosedAgain()
    {
        var account = Account.Create("Compte", Guid.NewGuid(), ValidIban);
        account.Close();

        Assert.Throws<AccountClosedException>(() => account.Rename("Nouveau nom"));
        Assert.Throws<AccountAlreadyClosedException>(account.Close);
    }

    [Fact]
    public void Create_WhenNameHasMaximumLength_CreatesAccount()
    {
        // Arrange

        var name = new string(
            'A',
            StringPropertyLengths.NameLength);

        var bankId = Guid.NewGuid();

        // Act

        var account = Account.Create(
            name,
            bankId,
            ValidIban);

        // Assert

        Assert.Equal(
            name,
            account.Name);
    }

    [Fact]
    public void Create_WhenNameExceedsMaximumLength_Throws()
    {
        // Arrange

        var name = new string(
            'A',
            StringPropertyLengths.NameLength + 1);

        var bankId = Guid.NewGuid();

        // Act

        var action = () => Account.Create(
            name,
            bankId,
            ValidIban);

        // Assert

        var exception =
            Assert.Throws<ArgumentException>(
                action);

        Assert.Equal(
            "name",
            exception.ParamName);
    }

    [Fact]
    public void Rename_WhenNameHasMaximumLength_RenamesAccount()
    {
        // Arrange

        var account = Account.Create(
            "Compte",
            Guid.NewGuid(),
            ValidIban);

        var name = new string(
            'A',
            StringPropertyLengths.NameLength);

        // Act

        account.Rename(name);

        // Assert

        Assert.Equal(
            name,
            account.Name);
    }

    [Fact]
    public void Rename_WhenNameExceedsMaximumLength_Throws()
    {
        // Arrange

        var account = Account.Create(
            "Compte",
            Guid.NewGuid(),
            ValidIban);

        var name = new string(
            'A',
            StringPropertyLengths.NameLength + 1);

        // Act

        var action = () =>
            account.Rename(name);

        // Assert

        var exception =
            Assert.Throws<ArgumentException>(
                action);

        Assert.Equal(
            "name",
            exception.ParamName);
    }
}

using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.ValueObjects;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Infrastructure.Persistence;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Fixtures;

internal static class TestData
{
    public static string Iban(
        long accountNumber)
    {
        var bban =
            $"3000600001{accountNumber:0000000000000}";

        var numericValue =
            $"{bban}152700"; // F = 15, R = 27, checksum temporaire = 00

        var remainder = 0;

        foreach (var character in numericValue)
        {
            remainder =
                (remainder * 10 + (character - '0')) % 97;
        }

        var checksum =
            98 - remainder;

        return $"FR{checksum:00}{bban}";
    }

    public static ApplicationUser User(
        string prefix = "user",
        Guid? id = null)
    {
        var value = id ?? Guid.NewGuid();
        return new ApplicationUser
        {
            Id = value,
            UserName = $"{prefix}.{value:N}@example.test",
            NormalizedUserName = $"{prefix}.{value:N}@example.test".ToUpperInvariant(),
            Email = $"{prefix}.{value:N}@example.test",
            NormalizedEmail = $"{prefix}.{value:N}@example.test".ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "First",
            LastName = "Last"
        };
    }

    public static Bank Bank(string name = "Bank", string bic = "BNPAFRPP")
        => BudgetManager.Domain.Entities.Bank.Create(name, Bic.Create(bic));

    public static Account Account(
        Guid bankId,
        string name = "Account",
        long? accountNumber = null)
        => Domain.Entities.Account.Create(name, bankId, Domain.ValueObjects.Iban.Create(Iban(accountNumber ?? 1)));

    public static BudgetCategory Category(
        string name = "Category",
        string? description = "Description")
        => BudgetCategory.Create(name, description);

    public static async Task<ApplicationUser> AddUserAsync(
        ApplicationDbContext context,
        string prefix = "user")
    {
        var user = User(prefix);
        context.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    public static async Task<(Bank Bank, Account Account)> AddBankAndAccountAsync(
        ApplicationDbContext context,
        string bankName = "Bank",
        string accountName = "Account",
        string bic = "BNPAFRPP",
        long? accountNumber = null)
    {
        var bank = Bank(bankName, bic);
        var account = Account(bank.Id, accountName, accountNumber);

        context.Add(bank);
        context.Add(account);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (bank, account);
    }

    public static async Task<(ApplicationUser Owner, Budget Budget, BudgetCategory Category, Bank Bank, Account Account)> AddBudgetGraphAsync(
        ApplicationDbContext context,
        TestCurrentUser currentUser)
    {
        var owner = User("owner");

        context.Add(owner);

        await context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        currentUser.UserId = owner.Id;

        var (bank, account) = await AddBankAndAccountAsync(
            context,
            bankName: "Main Bank",
            accountName: "Current Account");

        var category = Category(
            "Living",
            "Living expenses");

        var budget = Budget.Create(
            "Household",
            owner.Id);

        budget.AssociateCategory(
            category,
            owner.Id);

        budget.AddTransaction(
            category.Id,
            account.Id,
            "Salary",
            TransactionType.Income,
            2500m,
            PaymentMethod.Cash,
            null,
            owner.Id);

        context.Add(category);
        context.Add(budget);

        await context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        return (
            owner,
            budget,
            category,
            bank,
            account);
    }
}

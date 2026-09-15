using BudgetManager.Domain.Common;
using BudgetManager.Domain.Entities.Common;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Exceptions;
using BudgetManager.Domain.Extensions;
using BudgetManager.Domain.ValueObjects;

namespace BudgetManager.Domain.Entities;

public sealed class Transaction : Entity
{
    public string Name { get; private set; } = string.Empty;
    public TransactionType Type { get; private set; }
    public UDecimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }

    public decimal SignedAmount => Amount.ToDecimal() * ((int)Type);

    public Guid BudgetId { get; private set; }
    public Budget Budget { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public BudgetCategory Category { get; private set; } = null!;
    public Guid AccountId { get; private set; }
    public Account Account { get; private set; } = null!;
    public Guid? TransferAccountId { get; private set; }
    public Account? TransferAccount { get; private set; } = null!;

    private Transaction()
    {
    }

    private static Guid? ValidateMethod(PaymentMethod method, Guid accountId, Guid? transferAccountId)
    {
        method.ThrowIfNotValid(nameof(method));

        if (method != PaymentMethod.BankTransfer)
            return null;

        if (transferAccountId is null || transferAccountId == Guid.Empty)
            throw new ArgumentException("TransferAccountId cannot be empty.", nameof(transferAccountId));

        if (accountId == transferAccountId)
            throw new CannotTransferToSameAccountException();

        return transferAccountId;
    }
    internal static Transaction Create(Guid budgetId, Guid categoryId, Guid accountId, string name, TransactionType type, UDecimal amount, PaymentMethod method, Guid? transferAccountId)
    {
        if (budgetId == Guid.Empty)
            throw new ArgumentException("BudgetId cannot be empty.", nameof(budgetId));
        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId));
        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId cannot be empty.", nameof(accountId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > StringPropertyLengths.NameLength)
            throw new ArgumentException($"Name cannot be longer than {StringPropertyLengths.NameLength} characters.", nameof(name));
        type.ThrowIfNotValid(nameof(type));
        if (amount == 0)
            throw new ArgumentException("Amount must be greater than 0.", nameof(amount));
        transferAccountId = ValidateMethod(method, accountId, transferAccountId);

        return new Transaction
        {
            Id = Guid.NewGuid(),
            BudgetId = budgetId,
            CategoryId = categoryId,
            AccountId = accountId,
            Name = name.Trim(),
            Type = type,
            Amount = amount,
            Method = method,
            TransferAccountId = transferAccountId
        };
    }

    internal void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > StringPropertyLengths.NameLength)
            throw new ArgumentException($"Name cannot be longer than {StringPropertyLengths.NameLength} characters.", nameof(name));
        Name = name.Trim();
    }
    internal void ChangeAmount(UDecimal amount)
    {
        if (amount == 0)
            throw new ArgumentException("Amount must be greater than 0.", nameof(amount));
        Amount = amount;
    }
    internal void ChangeMethod(PaymentMethod method, Guid? transferAccountId)
    {
        transferAccountId = ValidateMethod(method, AccountId, transferAccountId);
        Method = method;
        TransferAccountId = transferAccountId;
    }
}

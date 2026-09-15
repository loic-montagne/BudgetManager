using BudgetManager.Domain.Common;
using BudgetManager.Domain.Entities.Common;
using BudgetManager.Domain.Entities.Interfaces;
using BudgetManager.Domain.Exceptions;
using BudgetManager.Domain.ValueObjects;

namespace BudgetManager.Domain.Entities;

public sealed class Account : Entity, IHasOptimisticConcurrencyToken
{
    public string Name { get; private set; } = string.Empty;
    public bool IsClosed { get; private set; }
    public Iban Iban { get; private set; } = null!;
    public Guid BankId { get; private set; }

    public Bank Bank { get; private set; } = null!;

    /// <summary>
    /// Gets the optimistic-concurrency token maintained by the persistence layer.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private Account()
    {
    }

    public static Account Create(string name, Guid bankId, Iban iban)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > StringPropertyLengths.NameLength)
            throw new ArgumentException($"Name cannot be longer than {StringPropertyLengths.NameLength} characters.", nameof(name));
        ArgumentNullException.ThrowIfNull(iban);
        if (bankId == Guid.Empty)
            throw new ArgumentException("BankId cannot be empty.", nameof(bankId));

        return new Account()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            BankId = bankId,
            Iban = iban
        };
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > StringPropertyLengths.NameLength)
            throw new ArgumentException($"Name cannot be longer than {StringPropertyLengths.NameLength} characters.", nameof(name));
        if (IsClosed)
            throw new AccountClosedException();
        Name = name.Trim();
    }
    public void Close()
    {
        if (IsClosed)
            throw new AccountAlreadyClosedException();
        IsClosed = true;
    }
}

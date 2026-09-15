using BudgetManager.Domain.Common;
using BudgetManager.Domain.Entities.Common;
using BudgetManager.Domain.Entities.Interfaces;
using BudgetManager.Domain.ValueObjects;

namespace BudgetManager.Domain.Entities;

public sealed class Bank : Entity, IHasOptimisticConcurrencyToken
{
    public string Name { get; private set; } = string.Empty;
    public Bic Bic { get; private set; } = null!;

    private readonly List<Account> _accounts = [];
    public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();

    /// <summary>
    /// Gets the optimistic-concurrency token maintained by the persistence layer.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private Bank()
    {
    }

    public static Bank Create(string name, Bic bic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > StringPropertyLengths.NameLength)
            throw new ArgumentException($"Name cannot be longer than {StringPropertyLengths.NameLength} characters.", nameof(name));
        ArgumentNullException.ThrowIfNull(bic);
        return new Bank
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Bic = bic
        };
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > StringPropertyLengths.NameLength)
            throw new ArgumentException($"Name cannot be longer than {StringPropertyLengths.NameLength} characters.", nameof(name));
        Name = name.Trim();
    }
    public void ChangeBic(Bic bic)
    {
        ArgumentNullException.ThrowIfNull(bic);
        Bic = bic;
    }
}

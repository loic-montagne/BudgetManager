using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Exceptions;
using BudgetManager.Domain.ValueObjects;
using Xunit;

namespace BudgetManager.Domain.Tests.Entities;

public sealed class AuditableEntityTests
{
    [Fact]
    public void MarkCreated_InitializesCreationAndUpdateMetadata()
    {
        var account = Account.Create("Compte", Guid.NewGuid(), Iban.Create("FR7630006000011234567890189"));
        var userId = Guid.NewGuid();
        var timestamp = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero);

        account.MarkCreated(timestamp, userId);

        Assert.Equal(userId, account.CreatedBy);
        Assert.Equal(timestamp, account.CreatedOn);
        Assert.Equal(userId, account.UpdatedBy);
        Assert.Equal(timestamp, account.UpdatedOn);
        Assert.Throws<InvalidOperationException>(() =>
            account.MarkCreated(timestamp, userId));
    }

    [Fact]
    public void MarkUpdated_WithoutUser_KeepsPreviousUpdater()
    {
        var account = Account.Create("Compte", Guid.NewGuid(), Iban.Create("FR7630006000011234567890189"));
        var userId = Guid.NewGuid();
        account.MarkCreated(DateTime.UnixEpoch, userId);
        var updatedOn = DateTime.UnixEpoch.AddDays(1);

        account.MarkUpdated(updatedOn, null);

        Assert.Equal(userId, account.UpdatedBy);
        Assert.Equal(updatedOn, account.UpdatedOn);
    }

    [Fact]
    public void MarkUpdated_WhenEntityWasNotCreated_Throws()
    {
        var account = Account.Create(
            "Compte",
            Guid.NewGuid(),
            Iban.Create("FR7630006000011234567890189"));

        Assert.Throws<InvalidOperationException>(() =>
            account.MarkUpdated(
                DateTimeOffset.UtcNow,
                Guid.NewGuid()));
    }

    [Fact]
    public void MarkUpdated_WhenDateIsBeforePreviousUpdate_Throws()
    {
        var account = Account.Create(
            "Compte",
            Guid.NewGuid(),
            Iban.Create("FR7630006000011234567890189"));

        var createdOn = new DateTimeOffset(
            2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

        account.MarkCreated(createdOn, Guid.NewGuid());
        account.MarkUpdated(
            createdOn.AddDays(2),
            Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            account.MarkUpdated(
                createdOn.AddDays(1),
                Guid.NewGuid()));
    }

    [Fact]
    public void MarkUpdated_WhenDateEqualsPreviousUpdate_IsAllowed()
    {
        var account = Account.Create(
            "Compte",
            Guid.NewGuid(),
            Iban.Create("FR7630006000011234567890189"));

        var timestamp = new DateTimeOffset(
            2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

        account.MarkCreated(timestamp, Guid.NewGuid());

        account.MarkUpdated(timestamp, Guid.NewGuid());

        Assert.Equal(timestamp, account.UpdatedOn);
    }
}

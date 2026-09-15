using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Exceptions;
using BudgetManager.Domain.Extensions;
using Xunit;

namespace BudgetManager.Domain.Tests.Entities;

public sealed class InternalGuardRegressionTests
{
    [Fact]
    public void EnsureIsOwner_WhenUserHasNoAccess_Throws()
    {
        var budget = Budget.Create("Budget", Guid.NewGuid());

        var action = () => budget.EnsureIsOwner(Guid.NewGuid());

        Assert.Throws<UserIsNotOwnerException>(action);
    }

    [Fact]
    public void EnsureIsOwner_WhenUserIsMemberButNotOwner_Throws()
    {
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        budget.GrantAccess(userId, Permission.View, ownerId);

        var action = () => budget.EnsureIsOwner(userId);

        Assert.Throws<UserIsNotOwnerException>(action);
    }

    [Fact]
    public void BudgetAccess_PromoteToOwner_WhenAlreadyOwner_Throws()
    {
        var access = BudgetAccess.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            true,
            Permission.None);

        var action = access.PromoteToOwner;

        Assert.Throws<UserAlreadyOwnerException>(action);
    }

    [Fact]
    public void BudgetAccess_DemoteFromOwner_WhenNotOwner_Throws()
    {
        var access = BudgetAccess.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            false,
            Permission.View);

        var action = access.DemoteFromOwner;

        Assert.Throws<UserNotAlreadyOwnerException>(action);
    }

    [Fact]
    public void BudgetAccess_PromoteThenDemote_ResetsExpectedPermissions()
    {
        var access = BudgetAccess.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            false,
            Permission.View);

        access.PromoteToOwner();
        Assert.True(access.IsOwner);
        Assert.Equal(Permission.All.GetPermissions(), access.GetPermissions());

        access.DemoteFromOwner();

        Assert.False(access.IsOwner);
        Assert.Equal(Permission.All.GetPermissions(), access.GetPermissions());
    }
}

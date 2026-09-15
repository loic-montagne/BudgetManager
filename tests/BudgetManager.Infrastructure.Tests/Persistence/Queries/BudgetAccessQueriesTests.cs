using BudgetManager.Domain.Enums;
using BudgetManager.Infrastructure.Persistence.Queries;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Persistence.Queries;

[Collection(SqlServerCollection.Name)]
public sealed class BudgetAccessQueriesTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task GetByKeyAsync_WhenCurrentUserIsOwner_ProjectsTargetPermissions()
    {
        // Arrange

        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var expectedOwnerDisplayName =
            $"{owner.FirstName} {owner.LastName}".Trim();

        var target =
            await TestData.AddUserAsync(
                Context,
                "target");

        budget.SetPermissions(
            target.Id,
            Permission.View | Permission.Edit,
            owner.Id);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new BudgetAccessQueries(
                Context);

        // Act

        var result =
            await queries.GetByKeyAsync(
                budget.Id,
                target.Id,
                owner.Id,
                Permission.Share,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.False(
            result.IsOwner);

        Assert.Equal(
            [Permission.View, Permission.Edit],
            result.Permissions.OrderBy(x => x).ToArray());

        Assert.Equal(
            expectedOwnerDisplayName,
            result.CreatedByName);

        Assert.Equal(
            expectedOwnerDisplayName,
            result.UpdatedByName);
    }

    [Fact]
    public async Task GetByKeyAsync_WhenTargetIsOwner_ReturnsAllPermissions()
    {
        // Arrange

        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var queries =
            new BudgetAccessQueries(
                Context);

        // Act

        var result =
            await queries.GetByKeyAsync(
                budget.Id,
                owner.Id,
                owner.Id,
                Permission.Share,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.True(
            result.IsOwner);

        Assert.Contains(
            Permission.View,
            result.Permissions);

        Assert.Contains(
            Permission.Share,
            result.Permissions);

        Assert.Contains(
            Permission.Lock,
            result.Permissions);
    }

    [Fact]
    public async Task GetByKeyAsync_WhenCurrentUserHasNoRequiredPermission_ReturnsNull()
    {
        // Arrange

        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var target =
            await TestData.AddUserAsync(
                Context,
                "target");

        var current =
            await TestData.AddUserAsync(
                Context,
                "current");

        budget.SetPermissions(
            target.Id,
            Permission.View,
            owner.Id);

        budget.SetPermissions(
            current.Id,
            Permission.View,
            owner.Id);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new BudgetAccessQueries(
                Context);

        // Act

        var result =
            await queries.GetByKeyAsync(
                budget.Id,
                target.Id,
                current.Id,
                Permission.Share,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBudgetIdAsync_ReturnsAllAccessesWhenCurrentUserCanShare()
    {
        // Arrange

        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var first =
            await TestData.AddUserAsync(
                Context,
                "first");

        var second =
            await TestData.AddUserAsync(
                Context,
                "second");

        budget.SetPermissions(
            first.Id,
            Permission.View,
            owner.Id);

        budget.SetPermissions(
            second.Id,
            Permission.Edit | Permission.Share,
            owner.Id);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new BudgetAccessQueries(
                Context);

        // Act

        var result =
            await queries.GetByBudgetIdAsync(
                budget.Id,
                owner.Id,
                Permission.Share,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            3,
            result.Count);

        Assert.Contains(
            result,
            x => x.User.Id == first.Id &&
                 x.Permissions.Contains(Permission.View));

        Assert.Contains(
            result,
            x => x.User.Id == second.Id &&
                 x.Permissions.Contains(Permission.Edit) &&
                 x.Permissions.Contains(Permission.Share));
    }
}

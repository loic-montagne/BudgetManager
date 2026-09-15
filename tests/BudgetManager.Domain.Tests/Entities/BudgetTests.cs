using BudgetManager.Domain.Common;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Exceptions;
using Xunit;

namespace BudgetManager.Domain.Tests.Entities;

public sealed class BudgetTests
{
    [Fact]
    public void Create_MakesCurrentUserOwnerWithAllPermissions()
    {
        var ownerId = Guid.NewGuid();

        var budget = Budget.Create("  Personnel  ", ownerId);

        Assert.Equal("Personnel", budget.Name);
        Assert.Single(budget.Accesses);
        Assert.True(budget.Accesses.Single().IsOwner);
        Assert.True(budget.Accesses.Single().HasAllPermissions(Permission.All));
    }

    [Fact]
    public void GrantAndRevokeAccess_UpdatesAccessCollection()
    {
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.GrantAccess(userId, Permission.View, ownerId);
        budget.GrantAccess(userId, Permission.Edit, ownerId);

        Assert.True(budget.HasAllPermissions(userId, Permission.View | Permission.Edit, ownerId));

        budget.RevokeAccess(userId, Permission.View, ownerId);
        Assert.False(budget.HasPermission(userId, Permission.View, ownerId));

        budget.RevokeAccess(userId, Permission.Edit, ownerId);
        Assert.DoesNotContain(budget.Accesses, access => access.UserId == userId);
    }

    [Fact]
    public void UserWithoutSharePermission_CannotGrantAccess()
    {
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var thirdUserId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        budget.GrantAccess(editorId, Permission.Edit, ownerId);

        Assert.Throws<UnauthorizedBudgetAccessException>(() =>
            budget.GrantAccess(thirdUserId, Permission.View, editorId));
    }

    [Fact]
    public void TransferOwnership_PromotesNewOwnerAndDemotesOldOwner()
    {
        var oldOwnerId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", oldOwnerId);
        budget.GrantAccess(newOwnerId, Permission.View, oldOwnerId);

        budget.TransferOwnership(newOwnerId, oldOwnerId);

        var oldOwner = budget.Accesses.Single(x => x.UserId == oldOwnerId);
        var newOwner = budget.Accesses.Single(x => x.UserId == newOwnerId);
        Assert.False(oldOwner.IsOwner);
        Assert.True(oldOwner.HasAllPermissions(Permission.All));
        Assert.True(newOwner.IsOwner);
    }

    [Fact]
    public void LockedBudget_CannotBeEdited()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        budget.Lock(ownerId);

        Assert.Throws<BudgetLockedException>(() => budget.Rename("Nouveau", ownerId));
        Assert.Throws<BudgetLockedException>(
            () => budget.AssociateCategory(
                BudgetCategory.Create(
                    "Courses",
                    null),
                ownerId));
    }

    [Fact]
    public void Category_CannotBeAddedTwice()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Courses", null);
        budget.AssociateCategory(category, ownerId);

        Assert.Throws<BudgetCategoryAlreadyAssociatedException>(() => budget.AssociateCategory(category, ownerId));
    }
    
    [Fact]
    public void Unlock_WhenBudgetIsLocked_UnlocksBudget()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        budget.Lock(ownerId);

        budget.Unlock(ownerId);

        Assert.False(budget.IsLocked);
    }

    [Fact]
    public void Lock_WhenUserHasNoLockPermission_ThrowsUnauthorizedBudgetAccessException()
    {
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.GrantAccess(userId, Permission.View, ownerId);

        Assert.Throws<UnauthorizedBudgetAccessException>(() =>
            budget.Lock(userId));
    }

    [Fact]
    public void Unlock_WhenUserHasNoLockPermission_ThrowsUnauthorizedBudgetAccessException()
    {
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.GrantAccess(userId, Permission.View, ownerId);

        Assert.Throws<UnauthorizedBudgetAccessException>(() =>
            budget.Unlock(userId));
    }

    [Fact]
    public void Lock_WhenBudgetIsAlreadyLocked_ThrowsBudgetAlreadyLockedException()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        budget.Lock(ownerId);

        Assert.Throws<BudgetAlreadyLockedException>(() =>
            budget.Lock(ownerId));
    }

    [Fact]
    public void Unlock_WhenBudgetIsAlreadyUnlocked_ThrowsBudgetAlreadyUnlockedException()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        Assert.Throws<BudgetAlreadyUnlockedException>(() =>
            budget.Unlock(ownerId));
    }

    [Fact]
    public void LockAndUnlock_UpdatesBudgetLockState()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.Lock(ownerId);
        Assert.True(budget.IsLocked);

        budget.Unlock(ownerId);
        Assert.False(budget.IsLocked);
    }

    [Fact]
    public void RemoveCategory_WhenUsedByTransaction_ThrowsBudgetCategoryInUseException()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Courses", null);

        budget.AssociateCategory(category, ownerId);
        budget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Courses",
            TransactionType.Expense,
            20m,
            PaymentMethod.CreditCard,
            null,
            ownerId);

        Assert.Throws<BudgetCategoryInUseException>(() =>
            budget.DissociateCategory(category.Id, ownerId));
    }

    [Fact]
    public void EnsureIsNotOwner_WhenUserIsOwner_Throws()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        Assert.Throws<UserIsOwnerException>(() =>
            budget.EnsureIsNotOwner(ownerId));
    }

    [Fact]
    public void EnsureIsNotOwner_WhenUserHasNoAccess_DoesNotThrow()
    {
        var budget = Budget.Create("Budget", Guid.NewGuid());

        budget.EnsureIsNotOwner(Guid.NewGuid());
    }

    [Fact]
    public void EnsureIsNotOwner_WhenUserHasAccessButIsNotOwner_DoesNotThrow()
    {
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.GrantAccess(
            userId,
            Permission.View,
            ownerId);

        budget.EnsureIsNotOwner(userId);
    }

    [Fact]
    public void SetPermissions_WhenAccessDoesNotExist_CreatesAccess()
    {
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.SetPermissions(
            userId,
            Permission.View | Permission.Edit,
            ownerId);

        var access = Assert.Single(
            budget.Accesses,
            access => access.UserId == userId);

        Assert.True(access.HasAllPermissions(
            Permission.View | Permission.Edit));
    }

    [Fact]
    public void SetPermissions_WhenAccessExists_ReplacesPermissions()
    {
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.GrantAccess(userId, Permission.View, ownerId);
        budget.GrantAccess(userId, Permission.Edit, ownerId);

        budget.SetPermissions(userId, Permission.Share, ownerId);

        Assert.False(
            budget.HasPermission(userId, Permission.View, ownerId));

        Assert.False(
            budget.HasPermission(userId, Permission.Edit, ownerId));

        Assert.True(
            budget.HasPermission(userId, Permission.Share, ownerId));
    }

    [Fact]
    public void SetPermissions_WithNone_RemovesExistingAccess()
    {
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.GrantAccess(userId, Permission.View, ownerId);

        budget.SetPermissions(
            userId,
            Permission.None,
            ownerId);

        Assert.DoesNotContain(
            budget.Accesses,
            access => access.UserId == userId);
    }

    [Fact]
    public void SetPermissions_WithNoneAndNoExistingAccess_DoesNothing()
    {
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.SetPermissions(
            userId,
            Permission.None,
            ownerId);

        Assert.DoesNotContain(
            budget.Accesses,
            access => access.UserId == userId);
    }

    [Fact]
    public void SetPermissions_WhenTargetIsOwner_Throws()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        Assert.Throws<CannotGrantOwnerPermissionsException>(() =>
            budget.SetPermissions(
                ownerId,
                Permission.View,
                ownerId));
    }

    [Fact]
    public void SetPermissions_WhenCurrentUserCannotShare_Throws()
    {
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.GrantAccess(
            editorId,
            Permission.Edit,
            ownerId);

        Assert.Throws<UnauthorizedBudgetAccessException>(() =>
            budget.SetPermissions(
                targetUserId,
                Permission.View,
                editorId));
    }

    [Fact]
    public void SetPermissions_WithUnknownPermission_Throws()
    {
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var invalid = (Permission)(1 << 8);

        Assert.Throws<ArgumentException>(() =>
            budget.SetPermissions(userId, invalid, ownerId));
    }

    [Fact]
    public void SetPermissions_WithEmptyUserId_Throws()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        Assert.Throws<ArgumentException>(() =>
            budget.SetPermissions(
                Guid.Empty,
                Permission.View,
                ownerId));
    }

    [Fact]
    public void SetPermissions_WithEmptyCurrentUserId_Throws()
    {
        var budget = Budget.Create("Budget", Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            budget.SetPermissions(
                Guid.NewGuid(),
                Permission.View,
                Guid.Empty));
    }

    [Fact]
    public void TransferOwnership_WhenTargetHasNoAccess_CreatesOwnerAccess()
    {
        // Arrange

        var oldOwnerId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            oldOwnerId);

        // Act

        budget.TransferOwnership(
            newOwnerId,
            oldOwnerId);

        // Assert

        var newOwnerAccess = Assert.Single(
            budget.Accesses,
            access => access.UserId == newOwnerId);

        Assert.True(newOwnerAccess.IsOwner);

        var oldOwnerAccess = Assert.Single(
            budget.Accesses,
            access => access.UserId == oldOwnerId);

        Assert.False(oldOwnerAccess.IsOwner);

        Assert.True(
            oldOwnerAccess.HasAllPermissions(
                Permission.All));
    }

    [Fact]
    public void TransferOwnership_WhenTargetIsCurrentOwner_Throws()
    {
        // Arrange

        var ownerId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        // Act

        var action = () => budget.TransferOwnership(
            ownerId,
            ownerId);

        // Assert

        Assert.Throws<CannotTransferOwnershipToYourselfException>(
            action);
    }

    [Fact]
    public void TransferOwnership_WhenCurrentUserIsNotOwner_Throws()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        budget.GrantAccess(
            memberId,
            Permission.Share,
            ownerId);

        // Act

        var action = () => budget.TransferOwnership(
            targetUserId,
            memberId);

        // Assert

        Assert.Throws<UnauthorizedBudgetAccessException>(
            action);
    }

    [Fact]
    public void GrantAccess_WhenPermissionIsAlreadyGranted_Throws()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        budget.GrantAccess(
            userId,
            Permission.View,
            ownerId);

        // Act

        var action = () => budget.GrantAccess(
            userId,
            Permission.View,
            ownerId);

        // Assert

        Assert.Throws<PermissionAlreadyGrantedException>(
            action);
    }

    [Fact]
    public void RevokeAccess_WhenPermissionIsNotGranted_Throws()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        budget.GrantAccess(
            userId,
            Permission.View,
            ownerId);

        // Act

        var action = () => budget.RevokeAccess(
            userId,
            Permission.Edit,
            ownerId);

        // Assert

        Assert.Throws<PermissionNotGrantedException>(
            action);
    }

    [Fact]
    public void DissociateCategory_WhenCategoryIsUnused_RemovesCategory()
    {
        // Arrange

        var ownerId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        var category = BudgetCategory.Create(
            "Courses",
            null);

        budget.AssociateCategory(
            category,
            ownerId);

        // Act

        budget.DissociateCategory(
            category.Id,
            ownerId);

        // Assert

        Assert.DoesNotContain(
            budget.Categories,
            item => item.Id == category.Id);
    }

    [Fact]
    public void RemoveTransaction_WhenTransactionIsNotAssociated_Throws()
    {
        // Arrange

        var ownerId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        // Act

        var action = () => budget.RemoveTransaction(
            Guid.NewGuid(),
            ownerId);

        // Assert

        Assert.Throws<BudgetTransactionNotAssociatedException>(
            action);
    }

    [Fact]
    public void Create_WhenNameHasMaximumLength_CreatesBudget()
    {
        // Arrange

        var ownerId = Guid.NewGuid();

        var name = new string(
            'B',
            StringPropertyLengths.NameLength);

        // Act

        var budget = Budget.Create(
            name,
            ownerId);

        // Assert

        Assert.Equal(
            name,
            budget.Name);
    }

    [Fact]
    public void Create_WhenNameExceedsMaximumLength_Throws()
    {
        // Arrange

        var ownerId = Guid.NewGuid();

        var name = new string(
            'B',
            StringPropertyLengths.NameLength + 1);

        // Act

        var action = () =>
            Budget.Create(
                name,
                ownerId);

        // Assert

        Assert.Throws<ArgumentException>(
            action);
    }

    [Fact]
    public void Rename_WhenNameExceedsMaximumLength_Throws()
    {
        // Arrange

        var ownerId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        var name = new string(
            'B',
            StringPropertyLengths.NameLength + 1);

        // Act

        var action = () =>
            budget.Rename(
                name,
                ownerId);

        // Assert

        Assert.Throws<ArgumentException>(
            action);
    }
}

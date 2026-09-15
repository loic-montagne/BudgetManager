using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Extensions;
using Xunit;

namespace BudgetManager.Domain.Tests.Extensions;

public sealed class PermissionExtensionsTests
{
    [Fact]
    public void GetPermissions_ReturnsIndividualFlags()
    {
        var permissions = (Permission.View | Permission.Share).GetPermissions();

        Assert.Equal([Permission.View, Permission.Share], permissions);
    }

    [Fact]
    public void Includes_RequiresSinglePermission()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Permission.All.Includes(Permission.View | Permission.Edit));
    }

    [Fact]
    public void InvalidFlag_IsRejected()
    {
        var invalid = (Permission)(1 << 8);

        Assert.False(invalid.IsValid());
        Assert.Throws<ArgumentException>(() => invalid.ThrowIfNotValid());
    }

    [Theory]
    [InlineData(Permission.None)]
    [InlineData(Permission.View)]
    [InlineData(Permission.View | Permission.Edit)]
    [InlineData(Permission.All)]
    public void IsValid_WithDefinedFlags_ReturnsTrue(
    Permission permission)
    {
        Assert.True(permission.IsValid());
    }

    [Fact]
    public void IsValid_WithValidAndUnknownFlags_ReturnsFalse()
    {
        var permissions =
            Permission.View | (Permission)(1 << 8);

        Assert.False(permissions.IsValid());
    }

    [Theory]
    [InlineData(Permission.View, true)]
    [InlineData(Permission.Edit, true)]
    [InlineData(Permission.None, false)]
    [InlineData(Permission.View | Permission.Edit, false)]
    [InlineData(Permission.All, false)]
    public void IsSinglePermission_ReturnsExpectedResult(
    Permission permission,
    bool expected)
    {
        Assert.Equal(
            expected,
            permission.IsSinglePermission());
    }

    [Fact]
    public void ThrowIfNone_WithNone_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Permission.None.ThrowIfNone());
    }

    [Theory]
    [InlineData(
    Permission.View | Permission.Edit,
    Permission.View,
    true)]
    [InlineData(
    Permission.View,
    Permission.View | Permission.Edit,
    false)]
    public void IncludesAll_ReturnsExpectedResult(
    Permission permissions,
    Permission expected,
    bool result)
    {
        Assert.Equal(result, permissions.IncludesAll(expected));
    }
}

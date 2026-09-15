using BudgetManager.Web.Common;
using BudgetManager.Web.Extensions;
using Xunit;

namespace BudgetManager.Web.Tests.Extensions;

public sealed class IEnumerableExtensionsTests
{
    private sealed record Item(string Text, string Value);

    [Fact]
    public void ToSelectListItemsList_WithValuesAndSpecialItems_ReturnsExpectedOrder()
    {
        var values = new[] { new Item("One", "1"), new Item("Two", "2") };
        var result = values.ToSelectListItemsList(x => x.Text, x => x.Value, true, "None", true, "All").ToArray();
        Assert.Equal(4, result.Length);
        Assert.Equal(Constants.NoneValue.ToString(), result[0].Value);
        Assert.Equal(Constants.AllValue.ToString(), result[1].Value);
        Assert.Equal("One", result[2].Text);
        Assert.Equal("2", result[3].Value);
    }

    [Fact]
    public void ToSelectListItemsList_WithNullValues_ReturnsEmpty()
    {
        IEnumerable<Item>? values = null;
        Assert.Empty(values.ToSelectListItemsList(x => x.Text, x => x.Value));
    }

    [Fact]
    public void ToSelectListItemsList_WithNullExpression_Throws()
    {
        var values = Array.Empty<Item>();
        Assert.Throws<ArgumentNullException>(() => values.ToSelectListItemsList(null!, x => x.Value));
        Assert.Throws<ArgumentNullException>(() => values.ToSelectListItemsList(x => x.Text, null!));
    }

    [Fact]
    public void ToString_WithValues_UsesApplicationSeparator()
    {
        IEnumerable<int> values = [1, 2];

        Assert.Equal(
            $"1{Constants.SeparatorChar}2",
            IEnumerableExtensions.ToString(values));
    }

    [Fact]
    public void ToString_WithNull_ReturnsEmpty()
    {
        IEnumerable<int>? values = null;

        Assert.Equal(
            string.Empty,
            IEnumerableExtensions.ToString(values));
    }
}

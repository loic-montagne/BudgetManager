using BudgetManager.Web.Extensions;
using Xunit;

namespace BudgetManager.Web.Tests.Extensions;

public sealed class ObjectExtensionsTests
{
    private sealed record Child(string Name);
    private sealed record Parent(Child? Child, int Number);

    [Fact]
    public void GetValue_WithNestedProperty_ReturnsValue()
        => Assert.Equal("value", new Parent(new Child("value"), 2).GetValue(x => x.Child!.Name));

    [Fact]
    public void GetValue_WithNullTarget_ReturnsDefault()
    {
        Parent? target = null;
        Assert.Equal("fallback", target.GetValue(x => x!.Child!.Name, "fallback"));
    }

    [Fact]
    public void GetValue_WithCompiledExpression_ReturnsValue()
        => Assert.Equal(3, new Parent(null, 2).GetValue(x => x.Number + 1));

    [Fact]
    public void GetValue_WhenExpressionThrows_ReturnsDefault()
        => Assert.Equal("fallback", new Parent(null, 2).GetValue(x => x.Child!.Name.ToUpperInvariant(), "fallback"));
}

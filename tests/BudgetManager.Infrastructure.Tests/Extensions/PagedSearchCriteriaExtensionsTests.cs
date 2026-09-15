using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Infrastructure.Extensions;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Extensions;

public sealed class PagedSearchCriteriaExtensionsTests
{
    [Fact]
    public void GetLikePatternSearchValues_WhenSearchIsNull_ReturnsEmpty()
    {
        // Arrange

        var criteria = new PagedSearchCriteria<BankSortField>(
            null,
            null,
            null,
            null);

        // Act

        var values = criteria
            .GetLikePatternSearchValues()
            .ToArray();

        // Assert

        Assert.Empty(values);
    }

    [Fact]
    public void GetLikePatternSearchValues_WhenSearchContainsSpaces_SplitsAndTrimsValues()
    {
        // Arrange

        var criteria = new PagedSearchCriteria<BankSortField>(
            "  Alpha   Beta ",
            null,
            null,
            null);

        // Act

        var values = criteria
            .GetLikePatternSearchValues()
            .ToArray();

        // Assert

        Assert.Equal(
            ["%Alpha%", "%Beta%"],
            values);
    }

    [Fact]
    public void GetLikePatternSearchValues_WhenSearchContainsLikeCharacters_EscapesThem()
    {
        // Arrange

        var criteria = new PagedSearchCriteria<BankSortField>(
            @"50% A_B [Test] C:\Data",
            null,
            null,
            null);

        // Act

        var values = criteria
            .GetLikePatternSearchValues()
            .ToArray();

        // Assert

        Assert.Equal(
            [
                @"%50\%%",
                @"%A\_B%",
                @"%\[Test]%",
                @"%C:\\Data%"
            ],
            values);
    }
}

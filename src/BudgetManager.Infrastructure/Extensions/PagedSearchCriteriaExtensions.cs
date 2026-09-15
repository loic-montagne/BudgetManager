using BudgetManager.Application.Common.Pagination;

namespace BudgetManager.Infrastructure.Extensions;

internal static class PagedSearchCriteriaExtensions
{
    internal const string EscapeLikeCharacter = "\\";

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(
                EscapeLikeCharacter,
                $"{EscapeLikeCharacter}{EscapeLikeCharacter}",
                StringComparison.Ordinal)
            .Replace(
                "%",
                $"{EscapeLikeCharacter}%",
                StringComparison.Ordinal)
            .Replace(
                "_",
                $"{EscapeLikeCharacter}_",
                StringComparison.Ordinal)
            .Replace(
                "[",
                $"{EscapeLikeCharacter}[",
                StringComparison.Ordinal);
    }

    internal static IEnumerable<string> GetLikePatternSearchValues<TSortFieldEnum>(this PagedSearchCriteria<TSortFieldEnum> criteria)
        where TSortFieldEnum : struct, Enum
    {
        return criteria
            .Search
           ?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
           ?.Select(s => $"%{EscapeLikePattern(s)}%")
           ?? [];
    }
}

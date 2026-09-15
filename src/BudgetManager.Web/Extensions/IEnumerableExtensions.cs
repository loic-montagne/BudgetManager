using BudgetManager.Web.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;

namespace BudgetManager.Web.Extensions;

public static class IEnumerableExtensions
{
    public static IEnumerable<SelectListItem> ToSelectListItemsList<T>(
        this IEnumerable<T>? values,
        Expression<Func<T, string>> textExpr,
        Expression<Func<T, string>> valueExpr,
        bool addNone = false,
        string noneText = "",
        bool addAll = false,
        string allText = "")
    {
        ArgumentNullException.ThrowIfNull(textExpr, nameof(textExpr));
        ArgumentNullException.ThrowIfNull(valueExpr, nameof(valueExpr));
        if (values == null)
            return [];

        var list = new List<SelectListItem>();
        if (addNone && !string.IsNullOrEmpty(noneText))
            list.Add(new SelectListItem(noneText, Constants.NoneValue.ToString()));
        if (addAll && !string.IsNullOrEmpty(allText))
            list.Add(new SelectListItem(allText, Constants.AllValue.ToString()));
        foreach (var value in values)
            list.Add(new SelectListItem(value.GetValue(textExpr), value.GetValue(valueExpr)));
        return list;
    }

    public static string ToString<T>(this IEnumerable<T>? values)
    {
        if (values == null)
            return string.Empty;
        return string.Join(Constants.SeparatorChar, values);
    }
}

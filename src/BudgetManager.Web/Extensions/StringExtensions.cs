using BudgetManager.Web.Common;
using System.Web;

namespace BudgetManager.Web.Extensions;

public static class StringExtensions
{
    public static int ToInt(this string? value, int defaultValue = default)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;
        return int.TryParse(value, out int i) ? i : defaultValue;
    }

    public static string? ToHtml(this string? text)
    {
        text = HttpUtility.HtmlEncode(text);
        text = text?.Replace("\r\n", "\r");
        text = text?.Replace("\n", "\r");
        text = text?.Replace("\r", "<br/>\r\n");
        text = text?.Replace("  ", "&nbsp;&nbsp;");
        return text;
    }

    public static string ToControllerName(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        if (value.EndsWith("Controller"))
            value = value[..value.LastIndexOf("Controller")];
        return value;
    }

    public static IEnumerable<string> ToEnumerable(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return [];

        var array = value
            .Split(Constants.SeparatorChar)
            .Where(x => !string.IsNullOrEmpty(x));

        if (array == null || !array.Any())
            return [];

        return array;
    }
    public static IEnumerable<T> ToEnumerable<T>(this string? value, Func<string, T> converterSelector, out bool isNoneSelected, out bool isAllSelected)
    {
        ArgumentNullException.ThrowIfNull(converterSelector);
        
        var strArray = ToEnumerable(value);

        isNoneSelected = strArray.Any(x => x == Constants.NoneValue.ToString());
        isAllSelected = strArray.Any(x => x == Constants.AllValue.ToString());

        return strArray
            .Where(x => x != Constants.NoneValue.ToString() &&
                        x != Constants.AllValue.ToString())
            .Select(converterSelector);
    }
}

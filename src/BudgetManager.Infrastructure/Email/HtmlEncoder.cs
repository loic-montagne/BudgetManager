using System.Net;

namespace BudgetManager.Infrastructure.Email;

internal static class HtmlEncoder
{
    public static string EncodeWithLineBreaks(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return WebUtility.HtmlEncode(value)
                         .Replace(Environment.NewLine, "<br />");
    }
}

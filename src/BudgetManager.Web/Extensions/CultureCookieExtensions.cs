using BudgetManager.Application.Common;
using Microsoft.AspNetCore.Localization;

namespace BudgetManager.Web.Extensions;

public static class CultureCookieExtensions
{
    public static void SetRequestCultureCookie(this HttpResponse response, string culture)
    {
        if (!SupportedCultures.All.Contains(culture))
            return;

        response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true
            });
    }
}

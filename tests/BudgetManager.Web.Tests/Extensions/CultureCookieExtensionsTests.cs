using BudgetManager.Application.Common;
using BudgetManager.Web.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Xunit;

namespace BudgetManager.Web.Tests.Extensions;

public sealed class CultureCookieExtensionsTests
{
    [Fact]
    public void SetRequestCultureCookie_WithSupportedCulture_AppendsCultureCookie()
    {
        var context = new DefaultHttpContext();
        var culture = SupportedCultures.All.First();
        context.Response.SetRequestCultureCookie(culture);
        var cookie = Assert.Single(context.Response.Headers.SetCookie);
        Assert.Contains(CookieRequestCultureProvider.DefaultCookieName, cookie);
        Assert.Contains(culture, cookie);
    }

    [Fact]
    public void SetRequestCultureCookie_WithUnsupportedCulture_DoesNotAppendCookie()
    {
        var context = new DefaultHttpContext();
        context.Response.SetRequestCultureCookie("xx-XX");
        Assert.Equal(0, context.Response.Headers.SetCookie.Count);
    }
}

using BudgetManager.Application.Common;
using BudgetManager.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Controllers;

public sealed class CultureControllerTests
{
    [Fact]
    public void SetCulture_WithLocalReturnUrl_SetsCookieAndReturnsLocalRedirect()
    {
        var controller = Create(true);
        var result = Assert.IsType<LocalRedirectResult>(controller.SetCulture(SupportedCultures.All.First(), "/home"));
        Assert.Equal("/home", result.Url);
        Assert.NotEqual(0, controller.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public void SetCulture_WithExternalReturnUrl_RedirectsToLogin()
    {
        var controller = Create(false);
        var result = Assert.IsType<RedirectToPageResult>(controller.SetCulture(SupportedCultures.All.First(), "https://example.org"));
        Assert.Equal("/Account/Login", result.PageName);
        Assert.Equal("Identity", result.RouteValues!["area"]);
    }

    private static CultureController Create(bool isLocal)
    {
        var url = Substitute.For<IUrlHelper>();
        url.IsLocalUrl(Arg.Any<string>()).Returns(isLocal);
        return new CultureController { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }, Url = url };
    }
}

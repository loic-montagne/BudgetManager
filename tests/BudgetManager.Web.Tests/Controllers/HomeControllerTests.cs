using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Exceptions;
using BudgetManager.Web.Controllers;
using BudgetManager.Web.Models.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Controllers;

public sealed class HomeControllerTests
{
    [Fact]
    public void Index_ReturnsView() => Assert.IsType<ViewResult>(Create().Index());

    [Fact]
    public void Error_WithoutException_Returns500() => AssertStatus(new Exception("error"), 500);

    [Fact]
    public void Error_WithBadRequest_Returns400() => AssertStatus(new BadRequestException("error"), 400);

    [Fact]
    public void Error_WithUnauthenticated_Returns401()
    {
        var user = Substitute.For<ICurrentUser>();
        AssertStatus(new UnauthenticatedException(user), 401);
    }

    [Fact]
    public void Error_WithForbidden_Returns403()
    {
        var user = Substitute.For<ICurrentUser>();
        AssertStatus(new ForbiddenAccessException(user, "Administrator"), 403);
    }

    [Fact]
    public void Error_WithNotFound_Returns404() => AssertStatus(new NotFoundException<object>(Guid.NewGuid()), 404);

    [Fact]
    public void Error_WithConcurrency_Returns409() => AssertStatus(new ConcurrencyException("error", new Exception("inner")), 409);

    private static void AssertStatus(Exception exception, int expected)
    {
        var controller = Create();
        var feature = Substitute.For<IExceptionHandlerPathFeature>();
        feature.Error.Returns(exception);
        feature.Path.Returns("/path");
        controller.HttpContext.Features.Set(feature);
        var result = Assert.IsType<ViewResult>(controller.Error());
        var model = Assert.IsType<ErrorModel>(result.Model);
        Assert.Equal(expected, model.StatusCode);
        Assert.Equal(expected, controller.Response.StatusCode);
    }

    private static HomeController Create()
    {
        var controller = new HomeController(Substitute.For<ILogger<HomeController>>());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }
}
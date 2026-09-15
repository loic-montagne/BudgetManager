using BudgetManager.Web.Controllers;
using BudgetManager.Web.Models.Bank;
using BudgetManager.Web.Models.BudgetCategory;
using BudgetManager.Web.Services;
using BudgetManager.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Controllers;

public sealed class ControllerActionTests
{
    [Fact]
    public void BankIndexAndGetAddPartial_ReturnExpectedViews()
    {
        var controller = new BankController(Sender(), Localizer(), Errors());
        Assert.IsType<ViewResult>(controller.Index());
        var partial = Assert.IsType<PartialViewResult>(controller.GetAddPartial());
        Assert.Equal("_BankPartial", partial.ViewName);
        Assert.False(Assert.IsType<bool>(controller.ViewData["Edit"]));
        Assert.IsType<BankFormModel>(partial.Model);
    }

    [Fact]
    public void BudgetCategoryIndexAndGetAddPartial_ReturnExpectedViews()
    {
        var controller = new BudgetCategoryController(Sender(), Localizer(), Errors());
        Assert.IsType<ViewResult>(controller.Index());
        var partial = Assert.IsType<PartialViewResult>(controller.GetAddPartial());
        Assert.Equal("_BudgetCategoryPartial", partial.ViewName);
        Assert.IsType<BudgetCategoryFormModel>(partial.Model);
    }

    [Fact]
    public async Task MutationActions_WithNullModels_Throw()
    {
        var account = Attach(new AccountController(Sender(), Localizer(), Errors()));
        var bank = Attach(new BankController(Sender(), Localizer(), Errors()));
        var category = Attach(new BudgetCategoryController(Sender(), Localizer(), Errors()));
        var user = Attach(new UserController(Options.Create(new IdentityTokenOptions { AccountActivationLifetime = TimeSpan.FromDays(7), EmailChangeLifetime = TimeSpan.FromDays(1), PasswordResetLifetime = TimeSpan.FromHours(1) }), Substitute.For<BudgetManager.Application.Abstractions.Localization.IDateTimeLocalizer>(), Sender(), Localizer(), Errors()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => account.PostAdd(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => account.PostEdit(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => bank.PostAdd(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => bank.PostEdit(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => category.PostAdd(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => category.PostEdit(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => user.PostAdd(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => user.PostEdit(null!));
    }

    private static ISender Sender() => Substitute.For<ISender>();
    private static IStringLocalizer<SharedResource> Localizer() => Substitute.For<IStringLocalizer<SharedResource>>();
    private static IBusinessErrorLocalizer Errors() => Substitute.For<IBusinessErrorLocalizer>();
    private static T Attach<T>(T controller) where T : Controller { var h = new DefaultHttpContext { RequestAborted = TestContext.Current.CancellationToken }; controller.ControllerContext = new ControllerContext { HttpContext = h }; return controller; }
}

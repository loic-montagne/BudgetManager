using BudgetManager.Infrastructure.Identity;
using BudgetManager.Web.Areas.Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Areas.Identity;

public sealed class IdentityManagePasswordTests
{
    [Fact]
    public async Task ChangePassword_OnGetWithoutUser_ReturnsNotFound()
    {
        var manager = IdentityTestFactory.UserManager();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns((ApplicationUser?)null);
        var model = IdentityTestFactory.Attach(Create(manager));

        Assert.IsType<NotFoundObjectResult>(await model.OnGetAsync());
    }

    [Fact]
    public async Task ChangePassword_OnGetWithoutPassword_RedirectsToSetPassword()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.HasPasswordAsync(user).Returns(false);
        var model = IdentityTestFactory.Attach(Create(manager));

        var result = Assert.IsType<RedirectToPageResult>(await model.OnGetAsync());

        Assert.Equal("./SetPassword", result.PageName);
    }

    [Fact]
    public async Task ChangePassword_OnPostWithInvalidModelState_DoesNotChangePassword()
    {
        var manager = IdentityTestFactory.UserManager();
        var model = IdentityTestFactory.Attach(Create(manager));
        model.Input = Input();
        model.ModelState.AddModelError("Password", "invalid");

        Assert.IsType<PageResult>(await model.OnPostAsync());
        await manager.DidNotReceiveWithAnyArgs().ChangePasswordAsync(default!, default!, default!);
    }

    [Fact]
    public async Task ChangePassword_OnPostWhenIdentityFails_AddsErrors()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.ChangePasswordAsync(user, "old", "new-password")
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Bad password" }));
        var model = IdentityTestFactory.Attach(Create(manager));
        model.Input = Input();

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState[string.Empty]!.Errors, x => x.ErrorMessage == "Bad password");
    }

    [Fact]
    public async Task ChangePassword_OnPostWhenSuccessful_RefreshesSignInAndRedirects()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.ChangePasswordAsync(user, "old", "new-password").Returns(IdentityResult.Success);
        var localizer = IdentityTestFactory.Localizer();
        localizer["Message.PasswordChanged"].Returns(new LocalizedString("Message.PasswordChanged", "Changed"));
        var model = IdentityTestFactory.Attach(Create(manager, signIn, localizer));
        model.Input = Input();

        var result = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync());

        Assert.Null(result.PageName);
        Assert.Equal("Changed", model.StatusMessage);
        await signIn.Received(1).RefreshSignInAsync(user);
    }

    private static ChangePasswordModel.InputModel Input()
        => new() { OldPassword = "old", NewPassword = "new-password", ConfirmPassword = "new-password" };

    private static ChangePasswordModel Create(
        UserManager<ApplicationUser> manager,
        SignInManager<ApplicationUser>? signIn = null,
        IStringLocalizer<SharedResource>? localizer = null)
        => new(
            manager,
            signIn ?? IdentityTestFactory.SignInManager(manager),
            Substitute.For<ILogger<ChangePasswordModel>>(),
            localizer ?? IdentityTestFactory.Localizer());
}

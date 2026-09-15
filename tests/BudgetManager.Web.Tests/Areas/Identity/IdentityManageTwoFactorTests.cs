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

public sealed class IdentityManageTwoFactorTests
{
    [Fact]
    public void ShowRecoveryCodes_WithoutCodes_Redirects()
    {
        var model = new ShowRecoveryCodesModel();

        var result = Assert.IsType<RedirectToPageResult>(model.OnGet());

        Assert.Equal("./TwoFactorAuthentication", result.PageName);
    }

    [Fact]
    public void ShowRecoveryCodes_WithCodes_ReturnsPage()
    {
        var model = new ShowRecoveryCodesModel { RecoveryCodes = ["one"] };

        Assert.IsType<PageResult>(model.OnGet());
    }

    [Fact]
    public async Task Disable2fa_OnGetWithoutUser_ReturnsNotFound()
    {
        var manager = IdentityTestFactory.UserManager();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns((ApplicationUser?)null);
        var model = IdentityTestFactory.Attach(new Disable2faModel(
            manager, Substitute.For<ILogger<Disable2faModel>>(), IdentityTestFactory.Localizer()));

        Assert.IsType<NotFoundObjectResult>(await model.OnGet());
    }

    [Fact]
    public async Task Disable2fa_OnGetWhenAlreadyDisabled_Throws()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.GetTwoFactorEnabledAsync(user).Returns(false);
        var model = IdentityTestFactory.Attach(new Disable2faModel(
            manager, Substitute.For<ILogger<Disable2faModel>>(), IdentityTestFactory.Localizer()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnGet());
    }

    [Fact]
    public async Task Disable2fa_OnPostWhenSuccessful_Redirects()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.SetTwoFactorEnabledAsync(user, false).Returns(IdentityResult.Success);
        var localizer = IdentityTestFactory.Localizer();
        localizer["Message.TwoFactorDisabled"].Returns(new LocalizedString("Message.TwoFactorDisabled", "Disabled"));
        var model = IdentityTestFactory.Attach(new Disable2faModel(
            manager, Substitute.For<ILogger<Disable2faModel>>(), localizer));

        var result = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync());

        Assert.Equal("./TwoFactorAuthentication", result.PageName);
        Assert.Equal("Disabled", model.StatusMessage);
    }

    [Fact]
    public async Task GenerateRecoveryCodes_OnGetWhen2faDisabled_Throws()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.GetTwoFactorEnabledAsync(user).Returns(false);
        var model = IdentityTestFactory.Attach(new GenerateRecoveryCodesModel(
            manager, Substitute.For<ILogger<GenerateRecoveryCodesModel>>(), IdentityTestFactory.Localizer()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnGetAsync());
    }

    [Fact]
    public async Task GenerateRecoveryCodes_OnPostGeneratesTenCodes()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.GetTwoFactorEnabledAsync(user).Returns(true);
        manager.GetUserIdAsync(user).Returns(user.Id.ToString());
        manager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)
            .Returns(new[] { "one", "two" });
        var localizer = IdentityTestFactory.Localizer();
        localizer["Message.RecoveryCodesGenerated"].Returns(new LocalizedString("Message.RecoveryCodesGenerated", "Generated"));
        var model = IdentityTestFactory.Attach(new GenerateRecoveryCodesModel(
            manager, Substitute.For<ILogger<GenerateRecoveryCodesModel>>(), localizer));

        var result = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync());

        Assert.Equal("./ShowRecoveryCodes", result.PageName);
        Assert.Equal(new[] { "one", "two" }, model.RecoveryCodes);
        Assert.Equal("Generated", model.StatusMessage);
        await manager.Received(1).GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
    }

    [Fact]
    public async Task ResetAuthenticator_OnPostResets2faAndRefreshesSignIn()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.GetUserIdAsync(user).Returns(user.Id.ToString());
        var localizer = IdentityTestFactory.Localizer();
        localizer["Message.AuthenticatorReset"].Returns(new LocalizedString("Message.AuthenticatorReset", "Reset"));
        var model = IdentityTestFactory.Attach(new ResetAuthenticatorModel(
            manager, signIn, Substitute.For<ILogger<ResetAuthenticatorModel>>(), localizer));

        var result = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync());

        Assert.Equal("./EnableAuthenticator", result.PageName);
        Assert.Equal("Reset", model.StatusMessage);
        await manager.Received(1).SetTwoFactorEnabledAsync(user, false);
        await manager.Received(1).ResetAuthenticatorKeyAsync(user);
        await signIn.Received(1).RefreshSignInAsync(user);
    }

    [Fact]
    public async Task TwoFactorAuthentication_OnGetLoadsState()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.GetAuthenticatorKeyAsync(user).Returns("key");
        manager.GetTwoFactorEnabledAsync(user).Returns(true);
        manager.CountRecoveryCodesAsync(user).Returns(4);
        signIn.IsTwoFactorClientRememberedAsync(user).Returns(true);
        var model = IdentityTestFactory.Attach(new TwoFactorAuthenticationModel(
            manager, signIn, Substitute.For<ILogger<TwoFactorAuthenticationModel>>(), IdentityTestFactory.Localizer()));

        Assert.IsType<PageResult>(await model.OnGetAsync());
        Assert.True(model.HasAuthenticator);
        Assert.True(model.Is2faEnabled);
        Assert.True(model.IsMachineRemembered);
        Assert.Equal(4, model.RecoveryCodesLeft);
    }

    [Fact]
    public async Task TwoFactorAuthentication_OnPostForgetsBrowser()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        var localizer = IdentityTestFactory.Localizer();
        localizer["Message.BrowserForgotten"].Returns(new LocalizedString("Message.BrowserForgotten", "Forgotten"));
        var model = IdentityTestFactory.Attach(new TwoFactorAuthenticationModel(
            manager, signIn, Substitute.For<ILogger<TwoFactorAuthenticationModel>>(), localizer));

        var result = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync());

        Assert.Null(result.PageName);
        Assert.Equal("Forgotten", model.StatusMessage);
        await signIn.Received(1).ForgetTwoFactorClientAsync();
    }
}

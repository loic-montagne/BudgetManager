using BudgetManager.Infrastructure.Identity;
using BudgetManager.Web.Areas.Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Encodings.Web;
using Xunit;

namespace BudgetManager.Web.Tests.Areas.Identity;

public sealed class IdentityEnableAuthenticatorTests
{
    [Fact]
    public async Task OnGet_WhenAuthenticatorKeyExists_FormatsKeyAndBuildsUri()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.GetAuthenticatorKeyAsync(user).Returns("ABCDEFGH1234");
        manager.GetEmailAsync(user).Returns(user.Email);
        var model = IdentityTestFactory.Attach(Create(manager));

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("abcd efgh 1234", model.SharedKey);
        Assert.Contains("otpauth://totp/", model.AuthenticatorUri);
        Assert.Contains("ABCDEFGH1234", model.AuthenticatorUri);
        await manager.DidNotReceive().ResetAuthenticatorKeyAsync(user);
    }

    [Fact]
    public async Task OnGet_WhenAuthenticatorKeyMissing_ResetsKey()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.GetAuthenticatorKeyAsync(user).Returns("", "NEWKEY12");
        manager.GetEmailAsync(user).Returns(user.Email);
        var model = IdentityTestFactory.Attach(Create(manager));

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        await manager.Received(1).ResetAuthenticatorKeyAsync(user);
        Assert.Equal("newk ey12", model.SharedKey);
    }

    [Fact]
    public async Task OnPost_WithInvalidVerificationCode_AddsModelErrorAndKeepsPage()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.GetAuthenticatorKeyAsync(user).Returns("ABCDEFGH");
        manager.GetEmailAsync(user).Returns(user.Email);
        manager.VerifyTwoFactorTokenAsync(
            user,
            manager.Options.Tokens.AuthenticatorTokenProvider,
            "123456").Returns(false);
        var localizer = IdentityTestFactory.Localizer();
        localizer["Error.InvalidVerificationCode"]
            .Returns(new Microsoft.Extensions.Localization.LocalizedString("Error.InvalidVerificationCode", "Invalid code"));
        var model = IdentityTestFactory.Attach(Create(manager, localizer));
        model.Input = new EnableAuthenticatorModel.InputModel { Code = "123-456" };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState["Input.Code"]!.Errors, x => x.ErrorMessage == "Invalid code");
        await manager.DidNotReceive().SetTwoFactorEnabledAsync(user, true);
    }

    [Fact]
    public async Task OnPost_WithValidCodeAndNoRecoveryCodes_GeneratesCodes()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.VerifyTwoFactorTokenAsync(
            user,
            manager.Options.Tokens.AuthenticatorTokenProvider,
            "123456").Returns(true);
        manager.GetUserIdAsync(user).Returns(user.Id.ToString());
        manager.CountRecoveryCodesAsync(user).Returns(0);
        manager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10).Returns(["one", "two"]);
        var model = IdentityTestFactory.Attach(Create(manager));
        model.Input = new EnableAuthenticatorModel.InputModel { Code = "123 456" };

        var result = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync());

        Assert.Equal("./ShowRecoveryCodes", result.PageName);
        Assert.Equal(["one", "two"], model.RecoveryCodes);
        await manager.Received(1).SetTwoFactorEnabledAsync(user, true);
        await manager.Received(1).GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
    }

    [Fact]
    public async Task OnPost_WithValidCodeAndExistingRecoveryCodes_RedirectsToTwoFactorPage()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.VerifyTwoFactorTokenAsync(
            user,
            manager.Options.Tokens.AuthenticatorTokenProvider,
            "123456").Returns(true);
        manager.GetUserIdAsync(user).Returns(user.Id.ToString());
        manager.CountRecoveryCodesAsync(user).Returns(2);
        var model = IdentityTestFactory.Attach(Create(manager));
        model.Input = new EnableAuthenticatorModel.InputModel { Code = "123456" };

        var result = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync());

        Assert.Equal("./TwoFactorAuthentication", result.PageName);
        await manager.DidNotReceiveWithAnyArgs().GenerateNewTwoFactorRecoveryCodesAsync(default!, default);
    }

    private static EnableAuthenticatorModel Create(
        UserManager<ApplicationUser> manager,
        Microsoft.Extensions.Localization.IStringLocalizer<SharedResource>? localizer = null)
        => new(
            manager,
            Substitute.For<ILogger<EnableAuthenticatorModel>>(),
            UrlEncoder.Default,
            localizer ?? IdentityTestFactory.Localizer());
}

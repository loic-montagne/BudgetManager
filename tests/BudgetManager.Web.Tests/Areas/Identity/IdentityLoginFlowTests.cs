using BudgetManager.Infrastructure.Identity;
using BudgetManager.Web.Areas.Identity.Pages.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Areas.Identity;

public sealed class IdentityLoginFlowTests
{
    [Fact]
    public async Task Login_OnPostWithInvalidModelState_DoesNotSignIn()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        var model = IdentityTestFactory.Attach(CreateLogin(manager, signIn));
        model.Input = new LoginModel.InputModel { Email = "bad", Password = "pwd" };
        model.ModelState.AddModelError("Email", "invalid");

        var result = await model.OnPostAsync("/home");

        Assert.IsType<PageResult>(result);
        await signIn.DidNotReceiveWithAnyArgs().PasswordSignInAsync(default(string)!, default(string)!, default, default);
    }

    [Fact]
    public async Task Login_OnPostWhenPasswordSucceeds_RedirectsLocally()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        var user = IdentityTestFactory.User();
        manager.FindByEmailAsync(user.Email!).Returns(user);
        signIn.PasswordSignInAsync(user.Email!, "pwd", true, true).Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var model = IdentityTestFactory.Attach(CreateLogin(manager, signIn));
        model.Input = new LoginModel.InputModel { Email = user.Email!, Password = "pwd", RememberMe = true };

        var result = Assert.IsType<Microsoft.AspNetCore.Mvc.LocalRedirectResult>(await model.OnPostAsync("/home"));

        Assert.Equal("/home", result.Url);
    }

    [Fact]
    public async Task Login_OnPostWhenTwoFactorRequired_RedirectsToTwoFactor()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        signIn.PasswordSignInAsync("user@example.test", "pwd", false, true)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);
        var model = IdentityTestFactory.Attach(CreateLogin(manager, signIn));
        model.Input = new LoginModel.InputModel { Email = "user@example.test", Password = "pwd" };

        var result = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(await model.OnPostAsync("/home"));

        Assert.Equal("./LoginWith2fa", result.PageName);
    }

    [Fact]
    public async Task Login_OnPostWhenLockedOut_RedirectsToLockout()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        signIn.PasswordSignInAsync("user@example.test", "pwd", false, true)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
        var model = IdentityTestFactory.Attach(CreateLogin(manager, signIn));
        model.Input = new LoginModel.InputModel { Email = "user@example.test", Password = "pwd" };

        var result = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(await model.OnPostAsync("/"));

        Assert.Equal("./Lockout", result.PageName);
    }

    [Fact]
    public async Task Login_OnPostWhenNotAllowed_AddsActivationError()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        var localizer = IdentityTestFactory.Localizer();
        localizer["Error.AccountNotActivated"].Returns(new LocalizedString("Error.AccountNotActivated", "Not activated"));
        signIn.PasswordSignInAsync("user@example.test", "pwd", false, true)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.NotAllowed);
        var model = IdentityTestFactory.Attach(CreateLogin(manager, signIn, localizer));
        model.Input = new LoginModel.InputModel { Email = "user@example.test", Password = "pwd" };

        var result = await model.OnPostAsync("/");

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState[string.Empty]!.Errors, x => x.ErrorMessage == "Not activated");
    }

    [Fact]
    public async Task Login_OnPostWithInvalidPassword_UsesRemainingAttempts()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        var localizer = IdentityTestFactory.Localizer();
        var user = IdentityTestFactory.User();
        manager.Options.Lockout.MaxFailedAccessAttempts = 5;
        manager.FindByEmailAsync(user.Email!).Returns(user);
        manager.GetAccessFailedCountAsync(user).Returns(2);
        signIn.PasswordSignInAsync(user.Email!, "pwd", false, true).Returns(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        localizer["Error.InvalidLoginRemainingAttempts", 3]
            .Returns(new LocalizedString("Error.InvalidLoginRemainingAttempts", "3 attempts"));

        var model = IdentityTestFactory.Attach(CreateLogin(manager, signIn, localizer));
        model.Input = new LoginModel.InputModel { Email = user.Email!, Password = "pwd" };

        var result = await model.OnPostAsync("/");

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState[string.Empty]!.Errors, x => x.ErrorMessage == "3 attempts");
    }

    [Fact]
    public async Task LoginWith2fa_OnGetWithoutPendingUser_Throws()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        signIn.GetTwoFactorAuthenticationUserAsync().Returns((ApplicationUser?)null);
        var model = IdentityTestFactory.Attach(new LoginWith2faModel(
            signIn, manager, Substitute.For<ILogger<LoginWith2faModel>>(), IdentityTestFactory.Localizer()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnGetAsync(false));
    }

    [Fact]
    public async Task LoginWith2fa_OnPostWhenLockedOut_RedirectsToLockout()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        var user = IdentityTestFactory.User();
        signIn.GetTwoFactorAuthenticationUserAsync().Returns(user);
        signIn.TwoFactorAuthenticatorSignInAsync("123456", false, false).Returns(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
        var model = IdentityTestFactory.Attach(new LoginWith2faModel(
            signIn, manager, Substitute.For<ILogger<LoginWith2faModel>>(), IdentityTestFactory.Localizer()));
        model.Input = new LoginWith2faModel.InputModel { TwoFactorCode = "123 456" };

        var result = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(await model.OnPostAsync(false, "/"));

        Assert.Equal("./Lockout", result.PageName);
    }

    [Fact]
    public async Task RecoveryCode_OnPostStripsSpacesBeforeSignIn()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        var user = IdentityTestFactory.User();
        signIn.GetTwoFactorAuthenticationUserAsync().Returns(user);
        signIn.TwoFactorRecoveryCodeSignInAsync("abcd1234").Returns(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        manager.GetUserIdAsync(user).Returns(user.Id.ToString());
        var localizer = IdentityTestFactory.Localizer();
        localizer["Error.InvalidRecoveryCode"].Returns(new LocalizedString("Error.InvalidRecoveryCode", "Invalid"));
        var model = IdentityTestFactory.Attach(new LoginWithRecoveryCodeModel(
            signIn, manager, Substitute.For<ILogger<LoginWithRecoveryCodeModel>>(), localizer));
        model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "abcd 1234" };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        await signIn.Received(1).TwoFactorRecoveryCodeSignInAsync("abcd1234");
    }

    private static LoginModel CreateLogin(
        UserManager<ApplicationUser> manager,
        SignInManager<ApplicationUser> signIn,
        IStringLocalizer<SharedResource>? localizer = null)
        => new(manager, signIn, Substitute.For<ILogger<LoginModel>>(), localizer ?? IdentityTestFactory.Localizer());
}

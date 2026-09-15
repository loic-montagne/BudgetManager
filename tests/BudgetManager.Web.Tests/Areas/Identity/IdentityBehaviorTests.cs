using BudgetManager.Application.Features.User.ConfirmEmailChange;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Web.Areas.Identity.Pages.Account;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Areas.Identity;

public sealed class IdentityBehaviorTests
{
    [Fact]
    public void ActivateAccount_OnGetWithoutCode_ReturnsBadRequest()
    {
        var localizer = Localizer();
        localizer["Error.ActivationCodeRequired"].Returns(new LocalizedString("Error.ActivationCodeRequired", "Code required"));
        var model = new ActivateAccountModel(UserManager(), Substitute.For<ISender>(), localizer, Substitute.For<BudgetManager.Web.Services.IBusinessErrorLocalizer>());

        var result = Assert.IsType<BadRequestObjectResult>(model.OnGet());

        Assert.Equal("Code required", result.Value);
    }

    [Fact]
    public void ActivateAccount_OnGetWithValidCode_DecodesCode()
    {
        var model = new ActivateAccountModel(UserManager(), Substitute.For<ISender>(), Localizer(), Substitute.For<BudgetManager.Web.Services.IBusinessErrorLocalizer>());
        var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes("token"));

        var result = model.OnGet(encoded);

        Assert.IsType<PageResult>(result);
        Assert.Equal("token", model.Input.ActivationCode);
    }

    [Fact]
    public void ResetPassword_OnGetWithoutCode_ReturnsBadRequest()
    {
        var localizer = Localizer();
        localizer["Error.PasswordResetCodeRequired"].Returns(new LocalizedString("Error.PasswordResetCodeRequired", "Reset code required"));
        var model = new ResetPasswordModel(UserManager(), localizer);

        var result = Assert.IsType<BadRequestObjectResult>(model.OnGet());

        Assert.Equal("Reset code required", result.Value);
    }

    [Fact]
    public void ResetPassword_OnGetWithValidCode_DecodesCode()
    {
        var model = new ResetPasswordModel(UserManager(), Localizer());
        var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes("reset-token"));

        var result = model.OnGet(encoded);

        Assert.IsType<PageResult>(result);
        Assert.Equal("reset-token", model.Input.Code);
    }

    [Fact]
    public async Task ConfirmEmail_OnGetWithoutParameters_RedirectsToIndex()
    {
        var model = new ConfirmEmailModel(UserManager(), Localizer());

        var result = Assert.IsType<RedirectToPageResult>(await model.OnGetAsync(null!, null!));

        Assert.Equal("/Index", result.PageName);
    }

    [Fact]
    public async Task ConfirmEmailChange_OnGetWithInvalidUserId_DoesNotSendCommand()
    {
        var sender = Substitute.For<ISender>();
        var localizer = Localizer();
        localizer["Message.EmailChangeConfirmationError"].Returns(new LocalizedString("Message.EmailChangeConfirmationError", "Invalid"));
        var model = Attach(new ConfirmEmailChangeModel(UserManager(), SignInManager(), localizer, sender));

        var result = await model.OnGetAsync("not-a-guid", "user@example.test", "code");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Invalid", model.StatusMessage);
        await sender.DidNotReceiveWithAnyArgs().Send(Arg.Any<ConfirmEmailChangeUserCommand>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ResetPassword_OnPostWithInvalidModelState_DoesNotQueryUser()
    {
        var manager = UserManager();
        var model = Attach(new ResetPasswordModel(manager, Localizer()));
        model.Input = new ResetPasswordModel.InputModel
        {
            Email = "user@example.test",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            Code = "code"
        };
        model.ModelState.AddModelError("Email", "invalid");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        await manager.DidNotReceiveWithAnyArgs().FindByEmailAsync(default!);
    }

    [Fact]
    public void ResendEmailConfirmation_OnGet_IsNoOp()
    {
        var model = new ResendEmailConfirmationModel(
            Substitute.For<ISender>(),
            UserManager(),
            Localizer(),
            Options.Create(new BudgetManager.Infrastructure.Configuration.IdentityTokenOptions
            {
                AccountActivationLifetime = TimeSpan.FromDays(7),
                EmailChangeLifetime = TimeSpan.FromDays(1),
                PasswordResetLifetime = TimeSpan.FromHours(1)
            }));

        model.OnGet();
    }

    private static T Attach<T>(T model) where T : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        model.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestAborted = TestContext.Current.CancellationToken
            }
        };
        return model;
    }

    private static UserManager<ApplicationUser> UserManager()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        return Substitute.For<UserManager<ApplicationUser>>(
            store,
            Options.Create(new IdentityOptions()),
            Substitute.For<IPasswordHasher<ApplicationUser>>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            Substitute.For<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<UserManager<ApplicationUser>>>());
    }

    private static SignInManager<ApplicationUser> SignInManager()
        => Substitute.For<SignInManager<ApplicationUser>>(
            UserManager(),
            Substitute.For<IHttpContextAccessor>(),
            Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Options.Create(new IdentityOptions()),
            Substitute.For<ILogger<SignInManager<ApplicationUser>>>(),
            Substitute.For<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>(),
            Substitute.For<IUserConfirmation<ApplicationUser>>());

    private static IStringLocalizer<SharedResource> Localizer()
        => Substitute.For<IStringLocalizer<SharedResource>>();
}

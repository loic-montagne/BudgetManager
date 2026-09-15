using BudgetManager.Application.Abstractions.Api;
using BudgetManager.Application.Abstractions.Email;
using BudgetManager.Application.Email;
using BudgetManager.Application.Email.Templates;
using BudgetManager.Application.Features.User.Activate;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Web.Areas.Identity.Pages.Account;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Areas.Identity;

public sealed class IdentityActivationAndPasswordTests
{
    [Fact]
    public async Task ForgotPassword_OnPostWithInvalidModelState_DoesNothing()
    {
        var manager = IdentityTestFactory.UserManager();
        var sender = Substitute.For<ITemplatedEmailSender>();
        var model = IdentityTestFactory.Attach(new ForgotPasswordModel(
            manager, sender, IdentityTestFactory.Localizer(), Substitute.For<IApplicationUrlBuilder>()));
        model.Input = new ForgotPasswordModel.InputModel { Email = "bad" };
        model.ModelState.AddModelError("Email", "invalid");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        await manager.DidNotReceiveWithAnyArgs().FindByEmailAsync(default!);
    }

    [Fact]
    public async Task ForgotPassword_OnPostWithUnknownUser_RedirectsWithoutSendingEmail()
    {
        var manager = IdentityTestFactory.UserManager();
        var sender = Substitute.For<ITemplatedEmailSender>();
        manager.FindByEmailAsync("unknown@example.test").Returns((ApplicationUser?)null);
        var model = IdentityTestFactory.Attach(new ForgotPasswordModel(
            manager, sender, IdentityTestFactory.Localizer(), Substitute.For<IApplicationUrlBuilder>()));
        model.Input = new ForgotPasswordModel.InputModel { Email = "unknown@example.test" };

        var result = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync());

        Assert.Equal("./ForgotPasswordConfirmation", result.PageName);
        await sender.DidNotReceiveWithAnyArgs().SendAsync(
            default!, default(PasswordResetEmailModel)!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ForgotPassword_OnPostWithUnconfirmedUser_RedirectsWithoutGeneratingToken()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.FindByEmailAsync(user.Email!).Returns(user);
        manager.IsEmailConfirmedAsync(user).Returns(false);
        var model = IdentityTestFactory.Attach(new ForgotPasswordModel(
            manager, Substitute.For<ITemplatedEmailSender>(), IdentityTestFactory.Localizer(), Substitute.For<IApplicationUrlBuilder>()));
        model.Input = new ForgotPasswordModel.InputModel { Email = user.Email! };

        var result = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync());

        Assert.Equal("./ForgotPasswordConfirmation", result.PageName);
        await manager.DidNotReceiveWithAnyArgs().GeneratePasswordResetTokenAsync(default!);
    }

    [Fact]
    public async Task ActivateAccount_OnPostWithInvalidModelState_DoesNotQueryUser()
    {
        var manager = IdentityTestFactory.UserManager();
        var model = IdentityTestFactory.Attach(new ActivateAccountModel(
            manager, Substitute.For<ISender>(), IdentityTestFactory.Localizer(), Substitute.For<IBusinessErrorLocalizer>()));
        model.Input = new ActivateAccountModel.InputModel
        {
            Email = "user@example.test",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            ActivationCode = "token"
        };
        model.ModelState.AddModelError("Email", "invalid");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        await manager.DidNotReceiveWithAnyArgs().FindByEmailAsync(default!);
    }

    [Fact]
    public async Task ActivateAccount_OnPostWithUnknownUser_RedirectsWithoutCommand()
    {
        var manager = IdentityTestFactory.UserManager();
        var sender = Substitute.For<ISender>();
        manager.FindByEmailAsync("unknown@example.test").Returns((ApplicationUser?)null);
        var model = IdentityTestFactory.Attach(new ActivateAccountModel(
            manager, sender, IdentityTestFactory.Localizer(), Substitute.For<IBusinessErrorLocalizer>()));
        model.Input = new ActivateAccountModel.InputModel
        {
            Email = "unknown@example.test",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            ActivationCode = "token"
        };

        var result = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync());

        Assert.Equal("./ActivateAccountConfirmation", result.PageName);
        await sender.DidNotReceiveWithAnyArgs().Send(
            Arg.Any<ActivateUserCommand>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ActivateAccount_OnPostWithAlreadyConfirmedUser_RedirectsWithoutCommand()
    {
        var manager = IdentityTestFactory.UserManager();
        var sender = Substitute.For<ISender>();
        var user = IdentityTestFactory.User();
        manager.FindByEmailAsync(user.Email!).Returns(user);
        manager.IsEmailConfirmedAsync(user).Returns(true);
        var model = IdentityTestFactory.Attach(new ActivateAccountModel(
            manager, sender, IdentityTestFactory.Localizer(), Substitute.For<IBusinessErrorLocalizer>()));
        model.Input = new ActivateAccountModel.InputModel
        {
            Email = user.Email!,
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            ActivationCode = "token"
        };

        var result = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync());

        Assert.Equal("./ActivateAccountConfirmation", result.PageName);
        await sender.DidNotReceiveWithAnyArgs().Send(
            Arg.Any<ActivateUserCommand>(), TestContext.Current.CancellationToken);
    }
}

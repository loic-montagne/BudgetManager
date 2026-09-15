using BudgetManager.Application.Abstractions.Api;
using BudgetManager.Application.Abstractions.Email;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Email;
using BudgetManager.Application.Email.Templates;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.User.ChangeEmail;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Web.Areas.Identity.Pages.Account.Manage;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using NSubstitute;
using System.Globalization;
using Xunit;

namespace BudgetManager.Web.Tests.Areas.Identity;

public sealed class IdentityEmailManagementTests
{
    [Fact]
    public async Task OnGet_LoadsEmailAndConfirmationState()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.GetEmailAsync(user).Returns(user.Email);
        manager.IsEmailConfirmedAsync(user).Returns(true);
        var model = IdentityTestFactory.Attach(Create(manager));

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(user.Email, model.Email);
        Assert.Equal(user.Email, model.Input.NewEmail);
        Assert.True(model.IsEmailConfirmed);
    }

    [Fact]
    public async Task OnPostChangeEmail_WithInvalidModelState_ReloadsWithoutOverwritingInput()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.GetEmailAsync(user).Returns(user.Email);
        manager.IsEmailConfirmedAsync(user).Returns(true);
        var sender = Substitute.For<ISender>();
        var model = IdentityTestFactory.Attach(Create(manager, sender: sender));
        model.Input = new EmailModel.InputModel { NewEmail = "invalid" };
        model.ModelState.AddModelError(nameof(model.Input.NewEmail), "invalid");

        var result = await model.OnPostChangeEmailAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("invalid", model.Input.NewEmail);
        Assert.Equal(user.Email, model.Email);
        await sender.DidNotReceiveWithAnyArgs().Send(
            Arg.Any<ChangeEmailUserCommand>(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OnPostChangeEmail_WhenSuccessful_SendsCommandAndVerificationEmail()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.GetEmailAsync(user).Returns(user.Email);

        var sender = Substitute.For<ISender>();
        sender.Send(
                Arg.Any<ChangeEmailUserCommand>(),
                TestContext.Current.CancellationToken)
            .Returns("email-token");

        var urlBuilder = Substitute.For<IApplicationUrlBuilder>();
        urlBuilder.GetPageUrl("/Account/ConfirmEmailChange", Arg.Any<object?>())
            .Returns("https://example.test/confirm");

        var emailSender = Substitute.For<ITemplatedEmailSender>();
        var localizer = IdentityTestFactory.Localizer();
        localizer["Email.ChangeEmail.Subject"]
            .Returns(new LocalizedString("Email.ChangeEmail.Subject", "Confirm change"));
        localizer["Message.EmailChangeVerificationSent"]
            .Returns(new LocalizedString("Message.EmailChangeVerificationSent", "Verification sent"));

        var model = IdentityTestFactory.Attach(Create(
            manager,
            sender,
            emailSender,
            localizer,
            urlBuilder));
        model.Input = new EmailModel.InputModel { NewEmail = "new@example.test" };

        var result = Assert.IsType<RedirectToPageResult>(await model.OnPostChangeEmailAsync());

        Assert.Null(result.PageName);
        Assert.Equal("Verification sent", model.StatusMessage);

        await sender.Received(1).Send(
            Arg.Is<ChangeEmailUserCommand>(x =>
                x.Id == user.Id &&
                x.OldEmail == user.Email &&
                x.NewEmail == "new@example.test"),
            TestContext.Current.CancellationToken);

        await emailSender.Received(1).SendAsync(
            Arg.Is<TemplatedEmailMessage>(x =>
                x.Subject == "Confirm change" &&
                x.To.Count == 1 &&
                x.To[0].Address == "new@example.test"),
            Arg.Is<EmailChangeConfirmationEmailModel>(x =>
                x.FirstName == user.FirstName &&
                x.ConfirmationUrl == "https://example.test/confirm"),
            Arg.Any<CultureInfo>(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OnPostChangeEmail_WhenValidationFails_MapsNewEmailErrorToInput()
    {
        var manager = IdentityTestFactory.UserManager();
        var user = IdentityTestFactory.User();
        manager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        manager.GetEmailAsync(user).Returns(user.Email);
        manager.IsEmailConfirmedAsync(user).Returns(true);

        var sender = Substitute.For<ISender>();
        var validationError = new ValidationError(
            nameof(ChangeEmailUserCommand.NormalizedNewEmail),
            "invalid",
            "User.Email.Invalid");
        sender.Send(
                Arg.Any<ChangeEmailUserCommand>(),
                TestContext.Current.CancellationToken)
            .Returns<Task<string>>(_ => throw new BadRequestException("invalid", [validationError]));

        var businessLocalizer = Substitute.For<IBusinessErrorLocalizer>();
        businessLocalizer.Localize(validationError).Returns("Localized invalid email");

        var model = IdentityTestFactory.Attach(Create(
            manager,
            sender,
            businessErrorLocalizer: businessLocalizer));
        model.Input = new EmailModel.InputModel { NewEmail = "bad@example.test" };

        var result = await model.OnPostChangeEmailAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains(
            model.ModelState[nameof(model.Input.NewEmail)]!.Errors,
            x => x.ErrorMessage == "Localized invalid email");
        Assert.Equal("bad@example.test", model.Input.NewEmail);
    }

    private static EmailModel Create(
        UserManager<ApplicationUser> manager,
        ISender? sender = null,
        ITemplatedEmailSender? emailSender = null,
        IStringLocalizer<SharedResource>? localizer = null,
        IApplicationUrlBuilder? urlBuilder = null,
        IBusinessErrorLocalizer? businessErrorLocalizer = null)
        => new(
            manager,
            IdentityTestFactory.SignInManager(manager),
            emailSender ?? Substitute.For<ITemplatedEmailSender>(),
            localizer ?? IdentityTestFactory.Localizer(),
            urlBuilder ?? Substitute.For<IApplicationUrlBuilder>(),
            businessErrorLocalizer ?? Substitute.For<IBusinessErrorLocalizer>(),
            sender ?? Substitute.For<ISender>());
}

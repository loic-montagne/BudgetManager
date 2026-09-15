using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.User.ConfirmEmailChange;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Web.Areas.Identity.Pages.Account;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using NSubstitute;
using System.Text;
using Xunit;

namespace BudgetManager.Web.Tests.Areas.Identity;

public sealed class IdentityConfirmEmailChangeTests
{
    [Fact]
    public async Task OnGet_WithInvalidEncodedCode_ShowsErrorWithoutSendingCommand()
    {
        var sender = Substitute.For<ISender>();
        var localizer = IdentityTestFactory.Localizer();
        localizer["Message.EmailChangeConfirmationError"]
            .Returns(new LocalizedString("Message.EmailChangeConfirmationError", "Invalid"));
        var model = IdentityTestFactory.Attach(Create(sender: sender, localizer: localizer));

        var result = await model.OnGetAsync(
            Guid.NewGuid().ToString(),
            "new@example.test",
            "%%%");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Invalid", model.StatusMessage);
        await sender.DidNotReceiveWithAnyArgs().Send(
            Arg.Any<ConfirmEmailChangeUserCommand>(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OnGet_WhenCommandSucceeds_SendsDecodedTokenAndRefreshesSignIn()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        var sender = Substitute.For<ISender>();
        var user = IdentityTestFactory.User("new@example.test");
        manager.FindByIdAsync(user.Id.ToString()).Returns(user);
        var localizer = IdentityTestFactory.Localizer();
        localizer["Message.EmailChangeConfirmed"]
            .Returns(new LocalizedString("Message.EmailChangeConfirmed", "Confirmed"));
        var model = IdentityTestFactory.Attach(Create(manager, signIn, sender, localizer));
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("decoded-token"));

        var result = await model.OnGetAsync(
            user.Id.ToString(),
            user.Email!,
            encoded);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Confirmed", model.StatusMessage);
        await sender.Received(1).Send(
            Arg.Is<ConfirmEmailChangeUserCommand>(x =>
                x.Id == user.Id &&
                x.Email == user.Email &&
                x.Token == "decoded-token"),
            TestContext.Current.CancellationToken);
        await signIn.Received(1).RefreshSignInAsync(user);
    }

    [Fact]
    public async Task OnGet_WhenUserNoLongerExists_StillShowsSuccessWithoutRefresh()
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        var userId = Guid.NewGuid();
        manager.FindByIdAsync(userId.ToString()).Returns((ApplicationUser?)null);
        var localizer = IdentityTestFactory.Localizer();
        localizer["Message.EmailChangeConfirmed"]
            .Returns(new LocalizedString("Message.EmailChangeConfirmed", "Confirmed"));
        var model = IdentityTestFactory.Attach(Create(
            manager,
            signIn,
            Substitute.For<ISender>(),
            localizer));
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("token"));

        var result = await model.OnGetAsync(
            userId.ToString(),
            "new@example.test",
            encoded);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Confirmed", model.StatusMessage);
        await signIn.DidNotReceiveWithAnyArgs().RefreshSignInAsync(default!);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnGet_WhenCommandFails_ShowsErrorAndDoesNotRefresh(bool badRequest)
    {
        var manager = IdentityTestFactory.UserManager();
        var signIn = IdentityTestFactory.SignInManager(manager);
        var sender = Substitute.For<ISender>();
        var localizer = IdentityTestFactory.Localizer();
        localizer["Message.EmailChangeConfirmationError"]
            .Returns(new LocalizedString("Message.EmailChangeConfirmationError", "Invalid"));

        if (badRequest)
        {
            sender.Send(
                    Arg.Any<ConfirmEmailChangeUserCommand>(),
                    TestContext.Current.CancellationToken)
                .Returns<Task>(_ => throw new BadRequestException("invalid"));
        }
        else
        {
            sender.Send(
                    Arg.Any<ConfirmEmailChangeUserCommand>(),
                    TestContext.Current.CancellationToken)
                .Returns<Task>(_ => throw new UpdateException("update failed", new InvalidOperationException()));
        }

        var model = IdentityTestFactory.Attach(Create(manager, signIn, sender, localizer));
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("token"));

        var result = await model.OnGetAsync(
            Guid.NewGuid().ToString(),
            "new@example.test",
            encoded);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Invalid", model.StatusMessage);
        await signIn.DidNotReceiveWithAnyArgs().RefreshSignInAsync(default!);
    }

    private static ConfirmEmailChangeModel Create(
        UserManager<ApplicationUser>? manager = null,
        SignInManager<ApplicationUser>? signIn = null,
        ISender? sender = null,
        IStringLocalizer<SharedResource>? localizer = null)
    {
        manager ??= IdentityTestFactory.UserManager();
        return new ConfirmEmailChangeModel(
            manager,
            signIn ?? IdentityTestFactory.SignInManager(manager),
            localizer ?? IdentityTestFactory.Localizer(),
            sender ?? Substitute.For<ISender>());
    }
}

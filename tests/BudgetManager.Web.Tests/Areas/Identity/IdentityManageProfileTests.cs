using BudgetManager.Application.Common;
using BudgetManager.Application.Features.User.GetCurrent;
using BudgetManager.Application.Features.User.UpdateProfile;
using BudgetManager.Application.Features.User.UpdateUiPreferences;
using BudgetManager.Web.Areas.Identity.Pages.Account.Manage;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Areas.Identity;

public sealed class IdentityManageProfileTests
{
    [Fact]
    public async Task OnGet_LoadsCurrentUserIntoForm()
    {
        var sender = Substitute.For<ISender>();
        var current = CurrentUser(preferredTheme: null);
        sender.Send(Arg.Any<GetCurrentUserQuery>(), TestContext.Current.CancellationToken).Returns(current);
        var model = IdentityTestFactory.Attach(Create(sender));

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(current.FirstName, model.Input.FirstName);
        Assert.Equal(current.LastName, model.Input.LastName);
        Assert.Equal(current.PhoneNumber, model.Input.PhoneNumber);
        Assert.Equal(current.PreferredCulture, model.Input.PreferredCulture);
        Assert.Equal(SupportedThemes.System, model.Input.PreferredTheme);
        Assert.NotEmpty(model.Input.Cultures);
        Assert.NotEmpty(model.Input.Themes);
        Assert.Equal(current.ProfilePictureContent, model.ProfilePictureContent);
        Assert.Equal(current.ProfilePictureContentType, model.ProfilePictureContentType);
    }

    [Fact]
    public async Task OnPostSave_WhenNothingChanged_DoesNotSendUpdateCommand()
    {
        var sender = Substitute.For<ISender>();
        var current = CurrentUser();
        sender.Send(Arg.Any<GetCurrentUserQuery>(), TestContext.Current.CancellationToken).Returns(current);
        var model = IdentityTestFactory.Attach(Create(sender));
        model.Input = InputFrom(current);

        var result = await model.OnPostSaveAsync();

        Assert.IsType<RedirectToPageResult>(result);
        await sender.DidNotReceiveWithAnyArgs().Send(
            Arg.Any<UpdateUserProfileCommand>(), TestContext.Current.CancellationToken);
        await sender.DidNotReceiveWithAnyArgs().Send(
            Arg.Any<UpdateUiPreferencesCommand>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OnPostSave_WhenOnlyPreferencesChanged_SendsPreferencesCommand()
    {
        var sender = Substitute.For<ISender>();
        var current = CurrentUser(preferredTheme: null);
        sender.Send(Arg.Any<GetCurrentUserQuery>(), TestContext.Current.CancellationToken).Returns(current);
        var model = IdentityTestFactory.Attach(Create(sender));
        model.Input = InputFrom(current);
        model.Input.PreferredCulture = "en-US";
        model.Input.PreferredTheme = SupportedThemes.System;

        var result = await model.OnPostSaveAsync();

        Assert.IsType<RedirectToPageResult>(result);
        await sender.Received(1).Send(
            Arg.Is<UpdateUiPreferencesCommand>(x =>
                x.Id == current.Id &&
                x.PreferredCulture == "en-US" &&
                x.PreferredTheme == null),
            TestContext.Current.CancellationToken);
        await sender.DidNotReceiveWithAnyArgs().Send(
            Arg.Any<UpdateUserProfileCommand>(), TestContext.Current.CancellationToken);
        Assert.True(model.SynchronizeUiPreferences);
    }

    [Fact]
    public async Task OnPostSave_WhenProfileChanged_SendsProfileCommand()
    {
        var sender = Substitute.For<ISender>();
        var current = CurrentUser();
        sender.Send(Arg.Any<GetCurrentUserQuery>(), TestContext.Current.CancellationToken).Returns(current);
        var model = IdentityTestFactory.Attach(Create(sender));
        model.Input = InputFrom(current);
        model.Input.FirstName = "Janet";

        var result = await model.OnPostSaveAsync();

        Assert.IsType<RedirectToPageResult>(result);
        await sender.Received(1).Send(
            Arg.Is<UpdateUserProfileCommand>(x =>
                x.Id == current.Id &&
                x.FirstName == "Janet" &&
                x.LastName == current.LastName),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OnPostSave_WithInvalidModelState_DoesNotSendUpdate()
    {
        var sender = Substitute.For<ISender>();
        var current = CurrentUser();
        sender.Send(Arg.Any<GetCurrentUserQuery>(), TestContext.Current.CancellationToken).Returns(current);
        var model = IdentityTestFactory.Attach(Create(sender));
        model.Input = InputFrom(current);
        model.ModelState.AddModelError(nameof(model.Input.FirstName), "invalid");

        var result = await model.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        await sender.DidNotReceiveWithAnyArgs().Send(
            Arg.Any<UpdateUserProfileCommand>(), TestContext.Current.CancellationToken);
        await sender.DidNotReceiveWithAnyArgs().Send(
            Arg.Any<UpdateUiPreferencesCommand>(), TestContext.Current.CancellationToken);
        Assert.Equal(current.FirstName, model.Input.FirstName);
    }

    private static IndexModel Create(ISender sender)
        => new(sender, IdentityTestFactory.Localizer(), Substitute.For<IBusinessErrorLocalizer>());

    private static IndexModel.InputModel InputFrom(CurrentUserDto user)
        => new()
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            PreferredCulture = user.PreferredCulture,
            PreferredTheme = user.PreferredTheme ?? SupportedThemes.System
        };

    private static CurrentUserDto CurrentUser(string? preferredTheme = "dark")
        => new(
            Guid.NewGuid(),
            "Jane",
            "Doe",
            "user@example.test",
            "+33600000000",
            [ApplicationRoles.User],
            "fr-FR",
            preferredTheme,
            [1, 2, 3],
            "image/png");
}

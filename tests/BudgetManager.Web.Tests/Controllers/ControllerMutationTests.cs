using BudgetManager.Application.Abstractions.Localization;
using BudgetManager.Application.Common;
using BudgetManager.Application.Features.Account.Close;
using BudgetManager.Application.Features.Account.Create;
using BudgetManager.Application.Features.Account.Delete;
using BudgetManager.Application.Features.Account.Update;
using BudgetManager.Application.Features.Bank.Create;
using BudgetManager.Application.Features.Bank.Delete;
using BudgetManager.Application.Features.Bank.Update;
using BudgetManager.Application.Features.BudgetCategory.Create;
using BudgetManager.Application.Features.BudgetCategory.Delete;
using BudgetManager.Application.Features.BudgetCategory.Update;
using BudgetManager.Application.Features.User.Create;
using BudgetManager.Application.Features.User.Delete;
using BudgetManager.Application.Features.User.SendActivationEmail;
using BudgetManager.Application.Features.User.Update;
using BudgetManager.Infrastructure.Configuration;
using BudgetManager.Web.Controllers;
using BudgetManager.Web.Models.Account;
using BudgetManager.Web.Models.Bank;
using BudgetManager.Web.Models.BudgetCategory;
using BudgetManager.Web.Models.User;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Controllers;

public sealed class ControllerMutationTests
{
    [Fact]
    public async Task AccountMutationActions_SendExpectedCommands()
    {
        var sender = Substitute.For<ISender>();
        var controller = Attach(new AccountController(sender, Localizer(), Errors()));
        var id = Guid.NewGuid();
        var bankId = Guid.NewGuid();
        var model = new AccountFormModel { Id = id, Name = "Checking", Iban = "FR761", BankId = bankId };

        await controller.PostAdd(model);
        await controller.PostEdit(model);
        await controller.Delete(id);
        await controller.Close(id);

        await sender.Received(1).Send(
            Arg.Is<CreateAccountCommand>(x => x.Name == "Checking" && x.Iban == "FR761" && x.BankId == bankId),
            TestContext.Current.CancellationToken);
        await sender.Received(1).Send(
            Arg.Is<UpdateAccountCommand>(x => x.Id == id && x.Name == "Checking"),
            TestContext.Current.CancellationToken);
        await sender.Received(1).Send(
            Arg.Is<DeleteAccountCommand>(x => x.Id == id),
            TestContext.Current.CancellationToken);
        await sender.Received(1).Send(
            Arg.Is<CloseAccountCommand>(x => x.Id == id),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AccountPostEdit_WithoutId_UsesEmptyGuid()
    {
        var sender = Substitute.For<ISender>();
        var controller = Attach(new AccountController(sender, Localizer(), Errors()));

        await controller.PostEdit(new AccountFormModel { Name = "Checking" });

        await sender.Received(1).Send(
            Arg.Is<UpdateAccountCommand>(x => x.Id == Guid.Empty && x.Name == "Checking"),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BankMutationActions_SendExpectedCommands()
    {
        var sender = Substitute.For<ISender>();
        var controller = Attach(new BankController(sender, Localizer(), Errors()));
        var id = Guid.NewGuid();
        var model = new BankFormModel { Id = id, Name = "Bank", Bic = "ABCDEFGH" };

        await controller.PostAdd(model);
        await controller.PostEdit(model);
        await controller.Delete(id);

        await sender.Received(1).Send(
            Arg.Is<CreateBankCommand>(x => x.Name == "Bank" && x.Bic == "ABCDEFGH"),
            TestContext.Current.CancellationToken);
        await sender.Received(1).Send(
            Arg.Is<UpdateBankCommand>(x => x.Id == id && x.Name == "Bank" && x.Bic == "ABCDEFGH"),
            TestContext.Current.CancellationToken);
        await sender.Received(1).Send(
            Arg.Is<DeleteBankCommand>(x => x.Id == id),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BudgetCategoryMutationActions_SendExpectedCommands()
    {
        var sender = Substitute.For<ISender>();
        var controller = Attach(new BudgetCategoryController(sender, Localizer(), Errors()));
        var id = Guid.NewGuid();
        var model = new BudgetCategoryFormModel { Id = id, Name = "Food", Description = "Groceries" };

        await controller.PostAdd(model);
        await controller.PostEdit(model);
        await controller.Delete(id);

        await sender.Received(1).Send(
            Arg.Is<CreateBudgetCategoryCommand>(x => x.Name == "Food" && x.Description == "Groceries"),
            TestContext.Current.CancellationToken);
        await sender.Received(1).Send(
            Arg.Is<UpdateBudgetCategoryCommand>(x => x.Id == id && x.Name == "Food" && x.Description == "Groceries"),
            TestContext.Current.CancellationToken);
        await sender.Received(1).Send(
            Arg.Is<DeleteBudgetCategoryCommand>(x => x.Id == id),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UserMutationActions_SendExpectedCommands()
    {
        var sender = Substitute.For<ISender>();
        var localizer = Localizer();
        localizer["Email.ActivateAccount.Subject"].Returns(
            new LocalizedString("Email.ActivateAccount.Subject", "Activate"));
        var controller = Attach(CreateUserController(sender, localizer));
        var id = Guid.NewGuid();
        var model = new UserFormModel
        {
            Id = id,
            Email = "user@example.com",
            LastName = "Doe",
            FirstName = "Jane",
            PhoneNumber = "+33600000000",
            Role = ApplicationRoles.User,
            PreferredCulture = "fr-FR",
            PreferredTheme = SupportedThemes.System
        };

        await controller.PostAdd(model);
        await controller.PostEdit(model);
        await controller.Delete(id);
        await controller.SendActivationEmail(id);

        await sender.Received(1).Send(
            Arg.Is<CreateUserCommand>(x =>
                x.Email == "user@example.com" &&
                x.LastName == "Doe" &&
                x.FirstName == "Jane" &&
                x.PhoneNumber == "+33600000000" &&
                x.Roles.SequenceEqual(new[] { ApplicationRoles.User }) &&
                x.PreferredCulture == "fr-FR" &&
                x.PreferredTheme == null &&
                x.ActivationPageName == "/Account/ActivateAccount" &&
                x.ActivationTokenLifetime == TimeSpan.FromDays(7) &&
                x.ActivationEmailSubject == "Activate"),
            TestContext.Current.CancellationToken);

        await sender.Received(1).Send(
            Arg.Is<UpdateUserCommand>(x =>
                x.Id == id &&
                x.LastName == "Doe" &&
                x.FirstName == "Jane" &&
                x.PhoneNumber == "+33600000000" &&
                x.Roles.SequenceEqual(new[] { ApplicationRoles.User }) &&
                x.PreferredCulture == "fr-FR" &&
                x.PreferredTheme == null),
            TestContext.Current.CancellationToken);

        await sender.Received(1).Send(
            Arg.Is<DeleteUserCommand>(x => x.Id == id),
            TestContext.Current.CancellationToken);

        await sender.Received(1).Send(
            Arg.Is<SendUserActivationEmailCommand>(x =>
                x.Id == id &&
                x.ActivationPageName == "/Account/ActivateAccount" &&
                x.TokenLifetime == TimeSpan.FromDays(7) &&
                x.Subject == "Activate"),
            TestContext.Current.CancellationToken);
    }

    private static UserController CreateUserController(
        ISender sender,
        IStringLocalizer<SharedResource>? localizer = null)
        => new(
            Options.Create(new IdentityTokenOptions
            {
                AccountActivationLifetime = TimeSpan.FromDays(7),
                EmailChangeLifetime = TimeSpan.FromDays(1),
                PasswordResetLifetime = TimeSpan.FromHours(1)
            }),
            Substitute.For<IDateTimeLocalizer>(),
            sender,
            localizer ?? Localizer(),
            Errors());

    private static IStringLocalizer<SharedResource> Localizer()
        => Substitute.For<IStringLocalizer<SharedResource>>();

    private static IBusinessErrorLocalizer Errors()
        => Substitute.For<IBusinessErrorLocalizer>();

    private static T Attach<T>(T controller) where T : Controller
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestAborted = TestContext.Current.CancellationToken
            }
        };
        return controller;
    }
}

using AccountByIdDto = BudgetManager.Application.Features.Account.GetById.AccountDto;
using AccountBankDto = BudgetManager.Application.Features.Account.GetById.BankDto;
using BankByIdDto = BudgetManager.Application.Features.Bank.GetById.BankDto;
using CategoryByIdDto = BudgetManager.Application.Features.BudgetCategory.GetById.BudgetCategoryDto;

using BudgetManager.Application.Features.Account.GetById;
using BudgetManager.Application.Features.Bank.GetAll;
using BudgetManager.Application.Features.Bank.GetById;
using BudgetManager.Application.Features.BudgetCategory.GetById;
using BudgetManager.Web.Controllers;
using BudgetManager.Web.Models.Account;
using BudgetManager.Web.Models.Bank;
using BudgetManager.Web.Models.BudgetCategory;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Controllers;

public sealed class EditPartialControllerTests
{
    [Fact]
    public async Task BankGetEditPartial_MapsDtoAndAuditFields()
    {
        var sender = Substitute.For<ISender>();
        var localizer = Localizer();
        localizer["Value.User.System"].Returns(new LocalizedString("Value.User.System", "System"));
        var id = Guid.NewGuid();
        var createdOn = DateTimeOffset.UtcNow.AddDays(-2);
        var updatedOn = DateTimeOffset.UtcNow.AddDays(-1);
        sender.Send(Arg.Any<GetBankByIdQuery>(), TestContext.Current.CancellationToken)
            .Returns(new BankByIdDto(id, "Bank", "ABCDEFGH", 2, Guid.NewGuid(), "", createdOn, Guid.NewGuid(), "Updater", updatedOn));
        var controller = Attach(new BankController(sender, localizer, Errors()));

        var result = Assert.IsType<PartialViewResult>(await controller.GetEditPartial(id));
        var model = Assert.IsType<BankFormModel>(result.Model);

        Assert.Equal("_BankPartial", result.ViewName);
        Assert.True(Assert.IsType<bool>(controller.ViewData["Edit"]));
        Assert.Equal(id, model.Id);
        Assert.Equal("Bank", model.Name);
        Assert.Equal("ABCDEFGH", model.Bic);
        Assert.Equal("System", model.CreatedBy);
        Assert.Equal("Updater", model.UpdatedBy);
        Assert.Equal(createdOn, model.CreatedOn);
        Assert.Equal(updatedOn, model.UpdatedOn);
        await sender.Received(1).Send(Arg.Is<GetBankByIdQuery>(x => x.Id == id), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BudgetCategoryGetEditPartial_MapsDto()
    {
        var sender = Substitute.For<ISender>();
        var id = Guid.NewGuid();
        sender.Send(Arg.Any<GetBudgetCategoryByIdQuery>(), TestContext.Current.CancellationToken)
            .Returns(new CategoryByIdDto(id, "Food", "Groceries", 3, Guid.NewGuid(), "Creator", DateTimeOffset.UtcNow, Guid.NewGuid(), "Updater", DateTimeOffset.UtcNow));
        var controller = Attach(new BudgetCategoryController(sender, Localizer(), Errors()));

        var result = Assert.IsType<PartialViewResult>(await controller.GetEditPartial(id));
        var model = Assert.IsType<BudgetCategoryFormModel>(result.Model);

        Assert.Equal("_BudgetCategoryPartial", result.ViewName);
        Assert.True(Assert.IsType<bool>(controller.ViewData["Edit"]));
        Assert.Equal(id, model.Id);
        Assert.Equal("Food", model.Name);
        Assert.Equal("Groceries", model.Description);
        Assert.Equal(3, model.BudgetsCount);
    }

    [Fact]
    public async Task AccountGetEditPartial_MapsAccountAndOrdersBanks()
    {
        var sender = Substitute.For<ISender>();
        var accountId = Guid.NewGuid();
        var bankId = Guid.NewGuid();
        sender.Send(Arg.Any<GetAccountByIdQuery>(), TestContext.Current.CancellationToken)
            .Returns(new AccountByIdDto(
                accountId, "Checking", false, "FR761",
                new AccountBankDto(bankId, "Beta", "BBBBBBBB"),
                Guid.NewGuid(), "Creator", DateTimeOffset.UtcNow,
                Guid.NewGuid(), "Updater", DateTimeOffset.UtcNow));
        sender.Send(Arg.Any<GetAllBanksQuery>(), TestContext.Current.CancellationToken)
            .Returns(new[]
            {
                new BudgetManager.Application.Features.Bank.GetAll.BankDto(bankId, "Beta", "BBBBBBBB", 1),
                new BudgetManager.Application.Features.Bank.GetAll.BankDto(Guid.NewGuid(), "Alpha", "AAAAAAAA", 0)
            });
        var controller = Attach(new AccountController(sender, Localizer(), Errors()));

        var result = Assert.IsType<PartialViewResult>(await controller.GetEditPartial(accountId));
        var model = Assert.IsType<AccountFormModel>(result.Model);

        Assert.Equal("_AccountPartial", result.ViewName);
        Assert.True(Assert.IsType<bool>(controller.ViewData["Edit"]));
        Assert.Equal(accountId, model.Id);
        Assert.Equal("Checking", model.Name);
        Assert.Equal("FR761", model.Iban);
        Assert.Equal(bankId, model.BankId);
        Assert.Equal(new[] { "Alpha", "Beta" }, model.Banks.Select(x => x.Text));
    }

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

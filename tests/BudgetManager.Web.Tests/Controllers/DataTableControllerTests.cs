using BudgetManager.Application.Abstractions.Localization;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Features.Bank.Search;
using BudgetManager.Application.Features.BudgetCategory.Search;
using BudgetManager.Application.Features.Account.Search;
using BudgetManager.Application.Features.User.Search;
using BudgetManager.Infrastructure.Configuration;
using BudgetManager.Web.Controllers;
using BudgetManager.Web.Models.Datatables;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Text.Json;
using Xunit;

namespace BudgetManager.Web.Tests.Controllers;

public sealed class DataTableControllerTests
{
    [Fact]
    public async Task BankGetDatatable_MapsModernRequestAndResponseContract()
    {
        var sender = Substitute.For<ISender>();
        var localizer = Substitute.For<IStringLocalizer<SharedResource>>();
        var errors = Substitute.For<IBusinessErrorLocalizer>();
        var dto = new BudgetManager.Application.Features.Bank.Search.BankDto(Guid.NewGuid(), "Bank", "ABCDEFGH", 3);
        sender.Send(Arg.Any<SearchBanksQuery>(), Arg.Any<CancellationToken>()).Returns(new PagedResult<BudgetManager.Application.Features.Bank.Search.BankDto>([dto], 10, 1, 1, 0, 25));
        var controller = Attach(new BankController(sender, localizer, errors));
        var result = await controller.GetDatatable(new DataTablesParameters { Draw = 4, Search = "ban", Start = 0, Length = 25, Orders = [new DataTablesOrder { Column = 0, Dir = "desc" }] });
        var json = JsonSerializer.Serialize(Assert.IsType<JsonResult>(result).Value);
        Assert.Contains("\"draw\":4", json);
        Assert.Contains("\"recordsTotal\":10", json);
        Assert.Contains("\"recordsFiltered\":1", json);
        Assert.Contains("\"data\"", json);
        Assert.Contains("Bank", json);
        await sender.Received(1).Send(Arg.Is<SearchBanksQuery>(x => x.Criteria.Search == "ban" && x.Criteria.Offset == 0 && x.Criteria.Limit == 25 && x.Criteria.Sorts!.Count == 1), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BudgetCategoryGetDatatable_MapsModernResponseContract()
    {
        var sender = Substitute.For<ISender>();
        var localizer = Substitute.For<IStringLocalizer<SharedResource>>();
        var errors = Substitute.For<IBusinessErrorLocalizer>();
        var dto = new BudgetCategoryDto(Guid.NewGuid(), "Food", "Desc", 2);
        sender.Send(Arg.Any<SearchBudgetCategoriesQuery>(), Arg.Any<CancellationToken>()).Returns(new PagedResult<BudgetCategoryDto>([dto], 2, 1, 1, 0, 10));
        var controller = Attach(new BudgetCategoryController(sender, localizer, errors));
        var result = await controller.GetDatatable(new DataTablesParameters { Draw = 1, Start = 0, Length = 10 });
        var json = JsonSerializer.Serialize(Assert.IsType<JsonResult>(result).Value);
        Assert.Contains("\"draw\":1", json);
        Assert.Contains("Food", json);
        Assert.Contains("Desc", json);
    }

    [Fact]
    public async Task AccountGetDatatable_MapsFiltersAndModernResponseContract()
    {
        var sender = Substitute.For<ISender>();
        var localizer = Substitute.For<IStringLocalizer<SharedResource>>();
        var errors = Substitute.For<IBusinessErrorLocalizer>();
        var bankId = Guid.NewGuid();
        var dto = new BudgetManager.Application.Features.Account.Search.AccountDto(Guid.NewGuid(), "Checking", false, "FR761", "Bank", "ABCDEFGH");
        sender.Send(Arg.Any<SearchAccountsQuery>(), Arg.Any<CancellationToken>()).Returns(new PagedResult<BudgetManager.Application.Features.Account.Search.AccountDto>([dto], 5, 1, 1, 0, 10));
        var controller = Attach(new AccountController(sender, localizer, errors));
        var result = await controller.GetDatatable(new DataTablesParameters { Draw = 2, Search = "check", Start = 0, Length = 10 }, false, bankId.ToString());
        var json = JsonSerializer.Serialize(Assert.IsType<JsonResult>(result).Value);
        Assert.Contains("\"draw\":2", json);
        Assert.Contains("Checking", json);
        await sender.Received(1).Send(Arg.Is<SearchAccountsQuery>(x => x.Criteria.IsClosed == false && !x.Criteria.AllBanks && x.Criteria.Banks.Contains(bankId) && x.Criteria.Search == "check"), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UserGetDatatable_MapsActivationFilterAndModernResponseContract()
    {
        var sender = Substitute.For<ISender>();
        var localizer = Substitute.For<IStringLocalizer<SharedResource>>();
        localizer[Arg.Any<string>()].Returns(x => new LocalizedString((string)x[0], (string)x[0], false));
        var errors = Substitute.For<IBusinessErrorLocalizer>();
        var dates = Substitute.For<IDateTimeLocalizer>();
        dates.ToLocalTime(Arg.Any<DateTimeOffset>()).Returns(x => (DateTimeOffset)x[0]);
        var dto = new BudgetManager.Application.Features.User.Search.UserDto(Guid.NewGuid(), "a@example.com", "Doe", "Jane", true, null, null, ["User"]);
        sender.Send(Arg.Any<SearchUsersQuery>(), Arg.Any<CancellationToken>()).Returns(new PagedResult<BudgetManager.Application.Features.User.Search.UserDto>([dto], 3, 1, 1, 0, 10));
        var options = Options.Create(new IdentityTokenOptions { AccountActivationLifetime = TimeSpan.FromDays(7), EmailChangeLifetime = TimeSpan.FromDays(1), PasswordResetLifetime = TimeSpan.FromHours(1) });
        var controller = Attach(new UserController(options, dates, sender, localizer, errors));
        var result = await controller.GetDatatable(new DataTablesParameters { Draw = 3, Start = 0, Length = 10 }, true);
        var json = JsonSerializer.Serialize(Assert.IsType<JsonResult>(result).Value);
        Assert.Contains("\"draw\":3", json);
        Assert.Contains("a@example.com", json);
        await sender.Received(1).Send(Arg.Is<SearchUsersQuery>(x => x.Criteria.IsActivated == true && x.Criteria.Offset == 0 && x.Criteria.Limit == 10), TestContext.Current.CancellationToken);
    }

    private static T Attach<T>(T controller) where T : Controller
    {
        var http = new DefaultHttpContext();
        http.RequestAborted = TestContext.Current.CancellationToken;
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }
}

using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Features.Account.Close;
using BudgetManager.Application.Features.Account.Create;
using BudgetManager.Application.Features.Account.Delete;
using BudgetManager.Application.Features.Account.GetById;
using BudgetManager.Application.Features.Account.Search;
using BudgetManager.Application.Features.Account.Update;
using BudgetManager.Application.Features.Bank.GetAll;
using BudgetManager.Web.Extensions;
using BudgetManager.Web.Models.Account;
using BudgetManager.Web.Models.Datatables;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Diagnostics;

namespace BudgetManager.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Administrator)]
public class AccountController(ISender sender, IStringLocalizer<SharedResource> sharedLocalizer, IBusinessErrorLocalizer businessErrorLocalizer) : SenderController(sender, sharedLocalizer, businessErrorLocalizer)
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var banks = await _sender.Send(new GetAllBanksQuery(), HttpContext.RequestAborted);

        ViewBag.StatesList = new[]
        {
            new { Value = "", Text = _sharedLocalizer["Value.All.Masculine"].Value },
            new { Value = "false", Text = _sharedLocalizer["Value.Active.Masculine.Plurial"].Value },
            new { Value = "true", Text = _sharedLocalizer["Value.Closed.Masculine.Plurial"].Value }
        }.ToSelectListItemsList(
            x => x.Text,
            x => x.Value)
         .ToList();
            
        ViewBag.BanksList = banks
            ?.OrderBy(x => x.Name)
            ?.ToSelectListItemsList(
                x => x.Name,
                x => x.Id.ToString())
            ?.ToList() ?? [];

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetAddPartial()
    {
        var banks = await _sender.Send(new GetAllBanksQuery(), HttpContext.RequestAborted);
        var bankItems = banks
            ?.OrderBy(x => x.Name)
            ?.ToSelectListItemsList(
                x => x.Name,
                x => x.Id.ToString())
            ?.ToList() ?? [];

        ViewData["Edit"] = false;

        return PartialView("_AccountPartial", new AccountFormModel()
        {
            BankId = Guid.TryParse(bankItems.FirstOrDefault()?.Value, out var selectedBankId)
                ? selectedBankId : Guid.Empty,
            Banks = bankItems
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetEditPartial(Guid id)
    {
        var account = await _sender.Send(new GetAccountByIdQuery(id), HttpContext.RequestAborted);
        var banks = await _sender.Send(new GetAllBanksQuery(), HttpContext.RequestAborted);
        var bankItems = banks
            ?.OrderBy(x => x.Name)
            ?.ToSelectListItemsList(
                x => x.Name,
                x => x.Id.ToString())
            ?.ToList() ?? [];


        ViewData["Edit"] = true;

        return PartialView("_AccountPartial", new AccountFormModel()
        {
            Id = account.Id,
            Name = account.Name,
            Iban = account.Iban,
            BankId = account.Bank.Id,
            Banks = bankItems,
            CreatedBy = string.IsNullOrWhiteSpace(account.CreatedByName) ? _sharedLocalizer["Value.User.System"] : account.CreatedByName,
            CreatedOn = account.CreatedOn,
            UpdatedBy = string.IsNullOrWhiteSpace(account.UpdatedByName) ? _sharedLocalizer["Value.User.System"] : account.UpdatedByName,
            UpdatedOn = account.UpdatedOn,
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetDatatable(DataTablesParameters param, bool? isClosed = null, string? banksIds = null)
    {
        var sorts = param.Orders?
            .Where(x => x.Column is >= 0 and <= 4)
            .Select(x => new SortCriterion<AccountSortField>(
                x.Column switch
                {
                    0 => AccountSortField.IsClosed,
                    1 => AccountSortField.Name,
                    2 => AccountSortField.BankName,
                    3 => AccountSortField.Iban,
                    4 => AccountSortField.Bic,
                    _ => throw new UnreachableException()
                },
                string.Equals(x.Dir, "desc", StringComparison.OrdinalIgnoreCase)
                    ? SortDirection.Descending
                    : SortDirection.Ascending))
            .ToList();

        var banks = banksIds
            .ToEnumerable(
                x => Guid.TryParse(x.Trim(), out var id) ? id : Guid.Empty,
                out var _,
                out var isAllBanksSelected)
            .ToList();
        if (banksIds is null || !banksIds.Any())
            isAllBanksSelected = true;

        var accounts = await _sender.Send(
            new SearchAccountsQuery(
                new PagedSearchCriteria(
                    isClosed,
                    isAllBanksSelected,
                    banks,
                    param.Search,
                    param.Start,
                    param.Length,
                    sorts)),
            HttpContext.RequestAborted);

        var data = accounts.Results
            .Select(account => new[]
            {
                string.Empty,          // Ajout de la colonne responsive
                account.IsClosed ? "0" : "1",
                account.Name,
                account.BankName,
                account.Iban,
                account.Bic,
                $"{account.Id}¤{(account.IsClosed ? "1" : "0")}"  // Ajout de la colonne d'actions
            })
            .ToArray();

        return Json(new
        {
            draw = param.Draw,
            recordsTotal = accounts.TotalCount,
            recordsFiltered = accounts.FilteredCount,
            data
        });
    }

    [HttpPost]
    public async Task<JsonResult> PostAdd(AccountFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return await Send(new CreateAccountCommand(model.Name, model.Iban, model.BankId));
    }

    [HttpPost]
    public async Task<JsonResult> PostEdit(AccountFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return await Send(new UpdateAccountCommand(model.Id ?? Guid.Empty, model.Name));
    }

    [HttpPost]
    public async Task<JsonResult> Delete(Guid id)
    {
        return await Send(new DeleteAccountCommand(id));
    }

    [HttpPost]
    public async Task<JsonResult> Close(Guid id)
    {
        return await Send(new CloseAccountCommand(id));
    }
}

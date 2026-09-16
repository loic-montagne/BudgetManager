using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Features.Bank.Create;
using BudgetManager.Application.Features.Bank.Delete;
using BudgetManager.Application.Features.Bank.GetById;
using BudgetManager.Application.Features.Bank.Search;
using BudgetManager.Application.Features.Bank.Update;
using BudgetManager.Web.Models.Bank;
using BudgetManager.Web.Models.Datatables;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Diagnostics;
using System.Globalization;

namespace BudgetManager.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Administrator)]
public class BankController(ISender sender, IStringLocalizer<SharedResource> sharedLocalizer, IBusinessErrorLocalizer businessErrorLocalizer) : SenderController(sender, sharedLocalizer, businessErrorLocalizer)
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult GetAddPartial()
    {
        ViewData["Edit"] = false;
        return PartialView("_BankPartial", new BankFormModel());
    }

    [HttpGet]
    public async Task<IActionResult> GetEditPartial(Guid id)
    {
        var bank = await _sender.Send(new GetBankByIdQuery(id), HttpContext.RequestAborted);

        ViewData["Edit"] = true;

        return PartialView("_BankPartial", new BankFormModel()
        {
            Id = bank.Id,
            Bic = bank.Bic,
            Name = bank.Name,
            CreatedBy = string.IsNullOrWhiteSpace(bank.CreatedByName) ? _sharedLocalizer["Value.User.System"] : bank.CreatedByName,
            CreatedOn = bank.CreatedOn,
            UpdatedBy = string.IsNullOrWhiteSpace(bank.UpdatedByName) ? _sharedLocalizer["Value.User.System"] : bank.UpdatedByName,
            UpdatedOn = bank.UpdatedOn,
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetDatatable(DataTablesParameters param)
    {
        var sorts = param.Orders?
            .Where(x => x.Column is >= 0 and <= 2)
            .Select(x => new SortCriterion<BankSortField>(
                x.Column switch
                {
                    0 => BankSortField.Name,
                    1 => BankSortField.Bic,
                    2 => BankSortField.AccountsCount,
                    _ => throw new UnreachableException()
                },
                string.Equals(x.Dir, "desc", StringComparison.OrdinalIgnoreCase)
                    ? SortDirection.Descending
                    : SortDirection.Ascending))
            .ToList();

        var banks = await _sender.Send(
            new SearchBanksQuery(
                new PagedSearchCriteria<BankSortField>(
                    param.Search,
                    param.Start,
                    param.Length,
                    sorts)),
            HttpContext.RequestAborted);

        var data = banks.Results
            .Select(bank => new[]
            {
                string.Empty,       // Ajout de la colonne responsive
                bank.Name,
                bank.Bic,
                bank.AccountsCount.ToString(CultureInfo.InvariantCulture),
                bank.Id.ToString()  // Ajout de la colonne d'actions
            })
            .ToArray();

        return Json(new
        {
            draw = param.Draw,
            recordsTotal = banks.TotalCount,
            recordsFiltered = banks.FilteredCount,
            data
        });
    }

    [HttpPost]
    public async Task<JsonResult> PostAdd(BankFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return await Send(new CreateBankCommand(model.Name, model.Bic));
    }

    [HttpPost]
    public async Task<JsonResult> PostEdit(BankFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return await Send(new UpdateBankCommand(model.Id ?? Guid.Empty, model.Name, model.Bic));
    }

    [HttpPost]
    public async Task<JsonResult> Delete(Guid id)
    {
        return await Send(new DeleteBankCommand(id));
    }
}

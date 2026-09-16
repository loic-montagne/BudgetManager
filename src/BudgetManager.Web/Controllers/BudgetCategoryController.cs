using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Features.BudgetCategory.Create;
using BudgetManager.Application.Features.BudgetCategory.Delete;
using BudgetManager.Application.Features.BudgetCategory.GetById;
using BudgetManager.Application.Features.BudgetCategory.Search;
using BudgetManager.Application.Features.BudgetCategory.Update;
using BudgetManager.Web.Extensions;
using BudgetManager.Web.Models.BudgetCategory;
using BudgetManager.Web.Models.Datatables;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Diagnostics;

namespace BudgetManager.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Administrator)]
public class BudgetCategoryController(ISender sender, IStringLocalizer<SharedResource> sharedLocalizer, IBusinessErrorLocalizer businessErrorLocalizer) : SenderController(sender, sharedLocalizer, businessErrorLocalizer)
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
        return PartialView("_BudgetCategoryPartial", new BudgetCategoryFormModel());
    }

    [HttpGet]
    public async Task<IActionResult> GetEditPartial(Guid id)
    {
        var category = await _sender.Send(new GetBudgetCategoryByIdQuery(id), HttpContext.RequestAborted);

        ViewData["Edit"] = true;

        return PartialView("_BudgetCategoryPartial", new BudgetCategoryFormModel()
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            BudgetsCount = category.BudgetsCount,
            CreatedBy = string.IsNullOrWhiteSpace(category.CreatedByName) ? _sharedLocalizer["Value.User.System"] : category.CreatedByName,
            CreatedOn = category.CreatedOn,
            UpdatedBy = string.IsNullOrWhiteSpace(category.UpdatedByName) ? _sharedLocalizer["Value.User.System"] : category.UpdatedByName,
            UpdatedOn = category.UpdatedOn,
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetDatatable(DataTablesParameters param)
    {
        var sorts = param.Orders?
            .Where(x => x.Column is >= 0 and <= 2)
            .Select(x => new SortCriterion<BudgetCategorySortField>(
                x.Column switch
                {
                    0 => BudgetCategorySortField.Name,
                    1 => BudgetCategorySortField.Description,
                    2 => BudgetCategorySortField.BudgetsCount,
                    _ => throw new UnreachableException()
                },
                string.Equals(x.Dir, "desc", StringComparison.OrdinalIgnoreCase)
                    ? SortDirection.Descending
                    : SortDirection.Ascending))
            .ToList();

        var categories = await _sender.Send(
            new SearchBudgetCategoriesQuery(
                new PagedSearchCriteria<BudgetCategorySortField>(
                    param.Search,
                    param.Start,
                    param.Length,
                    sorts)),
            HttpContext.RequestAborted);

        var data = categories.Results
            .Select(category => new[]
            {
                string.Empty,           // Ajout de la colonne responsive
                category.Name,
                category.Description.ToHtml(),
                category.BudgetsCount.ToString(),
                category.Id.ToString()  // Ajout de la colonne d'actions
            })
            .ToArray();

        return Json(new
        {
            draw = param.Draw,
            recordsTotal = categories.TotalCount,
            recordsFiltered = categories.FilteredCount,
            data
        });
    }

    [HttpPost]
    public async Task<JsonResult> PostAdd(BudgetCategoryFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return await Send(new CreateBudgetCategoryCommand(model.Name, model.Description));
    }

    [HttpPost]
    public async Task<JsonResult> PostEdit(BudgetCategoryFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return await Send(new UpdateBudgetCategoryCommand(model.Id ?? Guid.Empty, model.Name, model.Description));
    }

    [HttpPost]
    public async Task<JsonResult> Delete(Guid id)
    {
        return await Send(new DeleteBudgetCategoryCommand(id));
    }
}

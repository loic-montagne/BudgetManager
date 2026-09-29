using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.Budget.AssociateCategories;
using BudgetManager.Application.Features.Budget.Create;
using BudgetManager.Application.Features.Budget.Delete;
using BudgetManager.Application.Features.Budget.GetById;
using BudgetManager.Application.Features.Budget.Lock;
using BudgetManager.Application.Features.Budget.Unlock;
using BudgetManager.Application.Features.Budget.Update;
using BudgetManager.Application.Features.BudgetCategory.GetAll;
using BudgetManager.Web.Models.Budget;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BudgetManager.Web.Controllers;

[Authorize]
public class BudgetController(ISender sender, IStringLocalizer<SharedResource> sharedLocalizer, IBusinessErrorLocalizer businessErrorLocalizer) : SenderController(sender, sharedLocalizer, businessErrorLocalizer)
{
    [HttpGet]
    public async Task<IActionResult> Index(Guid id)
    {
        var budget = await _sender.Send(
            new GetBudgetByIdQuery(id),
            HttpContext.RequestAborted);

        return View(budget);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new BudgetFormModel());
    }

    [HttpGet]
    public async Task<IActionResult> GetEditPartial(Guid id)
    {
        var budget = await _sender.Send(new GetBudgetByIdQuery(id), HttpContext.RequestAborted);

        ViewData["Edit"] = true;
        ViewData["ShowDetails"] = false;

        return PartialView("_BudgetPartial", new BudgetFormModel()
        {
            Id = budget.Id,
            Name = budget.Name
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetDetailsPartial(Guid id)
    {
        var budget = await _sender.Send(new GetBudgetByIdQuery(id), HttpContext.RequestAborted);

        ViewData["Edit"] = true;
        ViewData["ShowDetails"] = true;

        return PartialView("_BudgetPartial", new BudgetFormModel()
        {
            Id = budget.Id,
            Name = budget.Name,
            CreatedBy = string.IsNullOrWhiteSpace(budget.CreatedByName) ? _sharedLocalizer["Value.User.System"] : budget.CreatedByName,
            CreatedOn = budget.CreatedOn,
            UpdatedBy = string.IsNullOrWhiteSpace(budget.UpdatedByName) ? _sharedLocalizer["Value.User.System"] : budget.UpdatedByName,
            UpdatedOn = budget.UpdatedOn,
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetCategoriesPartial(Guid id)
    {
        var budget = await _sender.Send(new GetBudgetByIdQuery(id), HttpContext.RequestAborted);
        var categories = await _sender.Send(new GetAllBudgetCategoriesQuery(), HttpContext.RequestAborted);
        categories = [.. categories.OrderBy(x => x.Name)];
               
        return PartialView("_CategoriesPartial", new BudgetCategoriesFormModel()
        {
            Id = budget.Id,
            Categories = [.. categories.Select(x => new BudgetCategoryFormModel()
            {
                Id = x.Id,
                Name= x.Name,
                IsAssociated = budget.Categories.Any(c => c.Id == x.Id),
            })]
        });
    }



    [HttpPost]
    public async Task<IActionResult> Create(BudgetFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            var budgetId = await _sender.Send(
                new CreateBudgetCommand(model.Name),
                HttpContext.RequestAborted);

            return RedirectToAction(nameof(Index), new { id = budgetId });
        }
        catch (BadRequestException ex)
        {
            foreach (var error in ex.ValidationErrors)
            {
                ModelState.AddModelError(
                    error.PropertyName,
                    _businessErrorLocalizer.Localize(error));
            }

            return View(model);
        }
    }

    [HttpPost]
    public async Task<JsonResult> Update(BudgetFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return await Send(new UpdateBudgetCommand(model.Id ?? Guid.Empty, model.Name));
    }

    [HttpPost]
    public async Task<JsonResult> Delete(Guid id)
    {
        return await Send(new DeleteBudgetCommand(id));
    }

    [HttpPost]
    public async Task<JsonResult> Lock(Guid id)
    {
        return await Send(new LockBudgetCommand(id));
    }

    [HttpPost]
    public async Task<JsonResult> Unlock(Guid id)
    {
        return await Send(new UnlockBudgetCommand(id));
    }

    [HttpPost]
    public async Task<JsonResult> AssociateCategories(BudgetCategoriesFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return await Send(new AssociateCategoriesCommand(model.Id, model.CategoriesIds));
    }

}

using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.Budget.Create;
using BudgetManager.Application.Features.Budget.GetById;
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

}

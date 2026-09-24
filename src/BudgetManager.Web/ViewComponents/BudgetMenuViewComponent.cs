using BudgetManager.Application.Features.Budget.GetAll;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Web.ViewComponents;

public sealed class BudgetMenuViewComponent(ISender sender) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var budgets = await sender.Send(
            new GetAllBudgetsQuery(),
            HttpContext.RequestAborted);

        return View(budgets);
    }
}

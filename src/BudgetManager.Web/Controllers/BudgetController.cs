using BudgetManager.Application.Common;
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
        return View();
    }

}

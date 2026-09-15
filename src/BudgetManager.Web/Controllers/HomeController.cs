using BudgetManager.Application.Exceptions;
using BudgetManager.Web.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BudgetManager.Web.Controllers;

public class HomeController(ILogger<HomeController> logger) : Controller()
{
    public IActionResult Index()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var statusCode =
            exceptionFeature?.Error switch
            {
                BadRequestException =>
                    StatusCodes.Status400BadRequest,

                UnauthenticatedException =>
                    StatusCodes.Status401Unauthorized,

                ForbiddenAccessException =>
                    StatusCodes.Status403Forbidden,

                NotFoundException =>
                    StatusCodes.Status404NotFound,

                ConcurrencyException =>
                    StatusCodes.Status409Conflict,

                _ =>
                    StatusCodes.Status500InternalServerError
            };

        if (exceptionFeature?.Error is Exception exception)
        {
            if (statusCode >= 500)
                logger.LogError(exception, "Unhandled exception while processing {Path}.", exceptionFeature.Path);
            else
                logger.LogWarning(exception, "Request failed with status code {StatusCode} on {Path}.", statusCode, exceptionFeature.Path);
        }

        Response.StatusCode = statusCode;

        return View(new ErrorModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier, statusCode));
    }
}

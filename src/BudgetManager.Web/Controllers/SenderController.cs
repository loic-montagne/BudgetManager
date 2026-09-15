using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Web.Extensions;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BudgetManager.Web.Controllers;

public abstract class SenderController(ISender sender, IStringLocalizer<SharedResource> sharedLocalizer, IBusinessErrorLocalizer businessErrorLocalizer) : Controller
{
    protected ISender _sender = sender;
    protected IStringLocalizer<SharedResource> _sharedLocalizer = sharedLocalizer;
    protected IBusinessErrorLocalizer _businessErrorLocalizer = businessErrorLocalizer;

    protected async Task<JsonResult> Send<TResponse>(ICommand<TResponse> command, Func<string, string>? propertyNameMapper = null, CancellationToken? cancellationToken = null)
    {
        cancellationToken ??= HttpContext.RequestAborted;
        return Json(await Send(command, propertyNameMapper, _sender, _sharedLocalizer, _businessErrorLocalizer, cancellationToken.Value));
    }

    protected async Task<JsonResult> Send(ICommand command, Func<string, string>? propertyNameMapper = null, CancellationToken? cancellationToken = null)
    {
        cancellationToken ??= HttpContext.RequestAborted;
        return Json(await Send(command, propertyNameMapper, _sender, _sharedLocalizer, _businessErrorLocalizer, cancellationToken.Value));
    }


    internal static async Task<dynamic> Send<TResponse>(ICommand<TResponse> command, Func<string, string>? propertyNameMapper, ISender sender, IStringLocalizer<SharedResource> sharedLocalizer, IBusinessErrorLocalizer businessErrorLocalizer, CancellationToken cancellationToken)
    {
        return await Send(async ct =>
        {
            var result = await sender.Send(command, ct);
            return new
            {
                Success = true,
                Result = result
            };
        },
        propertyNameMapper,
        sharedLocalizer, 
        businessErrorLocalizer, 
        cancellationToken);
    }

    internal static async Task<dynamic> Send(ICommand command, Func<string, string>? propertyNameMapper, ISender sender, IStringLocalizer<SharedResource> sharedLocalizer, IBusinessErrorLocalizer businessErrorLocalizer, CancellationToken cancellationToken)
    {
        return await Send(async ct =>
        {
            await sender.Send(command, ct);
            return new
            {
                Success = true
            };
        },
        propertyNameMapper,
        sharedLocalizer,
        businessErrorLocalizer,
        cancellationToken);
    }

    private static async Task<dynamic> Send(Func<CancellationToken, Task<dynamic>> func, Func<string, string>? propertyNameMapper, IStringLocalizer<SharedResource> sharedLocalizer, IBusinessErrorLocalizer businessErrorLocalizer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(func);

        try
        {
            return await func(cancellationToken);
        }
        catch (Application.Exceptions.BadRequestException ex)
        {
            var fieldErrors = ex.ValidationErrors
                .Where(x => !string.IsNullOrWhiteSpace(x.PropertyName))
                .ToArray();

            var generalErrors = ex.ValidationErrors
                .Where(x => string.IsNullOrWhiteSpace(x.PropertyName))
                .ToArray();

            return new
            {
                Success = false,

                InvalidControls = fieldErrors
                    .Select(x => new
                    {
                        Name = propertyNameMapper?.Invoke(x.PropertyName) ?? x.PropertyName,
                        Text = businessErrorLocalizer.Localize(x)
                    })
                    .ToArray(),

                Error = generalErrors.Length > 0
                    ? string.Join(
                        Environment.NewLine,
                        generalErrors.Select(businessErrorLocalizer.Localize))
                        .ToHtml()
                    : null
            };
        }
        catch (Application.Exceptions.Common.ApplicationException ex)
        {
            return new
            {
                Success = false,
                Error = ex.Message.ToHtml()
            };
        }
        catch (Exception ex)
        {
            return new
            {
                Success = false,
                Error = string.Format(sharedLocalizer["Error.NotManagedGeneric"], ex.Message).ToHtml()
            };
        }
    }
}

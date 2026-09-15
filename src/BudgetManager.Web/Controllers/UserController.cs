using BudgetManager.Application.Abstractions.Localization;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Features.User.Create;
using BudgetManager.Application.Features.User.Delete;
using BudgetManager.Application.Features.User.GetById;
using BudgetManager.Application.Features.User.Search;
using BudgetManager.Application.Features.User.SendActivationEmail;
using BudgetManager.Application.Features.User.Update;
using BudgetManager.Infrastructure.Configuration;
using BudgetManager.Web.Extensions;
using BudgetManager.Web.Models.Datatables;
using BudgetManager.Web.Models.User;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;

namespace BudgetManager.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Administrator)]
public class UserController(IOptions<IdentityTokenOptions> options, IDateTimeLocalizer dateTimeLocalizer, ISender sender, IStringLocalizer<SharedResource> sharedLocalizer, IBusinessErrorLocalizer businessErrorLocalizer) : SenderController(sender, sharedLocalizer, businessErrorLocalizer)
{
    private readonly IOptions<IdentityTokenOptions> _options = options;
    private readonly IDateTimeLocalizer _dateTimeLocalizer = dateTimeLocalizer;

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.StatesList = new[]
        {
            new { Value = "", Text = _sharedLocalizer["Value.All.Masculine"].Value },
            new { Value = "true", Text = _sharedLocalizer["Value.Active.Masculine.Plurial"].Value },
            new { Value = "false", Text = _sharedLocalizer["Value.NonActive.Masculine.Plurial"].Value }
        }.ToSelectListItemsList(
            x => x.Text,
            x => x.Value)
         .ToList();

        return View();
    }

    [HttpGet]
    public IActionResult GetAddPartial()
    {
        ViewData["Edit"] = false;

        var model = CreateUserFormModel();
        model.Role = ApplicationRoles.User;

        return PartialView("_UserPartial", model);
    }

    [HttpGet]
    public async Task<IActionResult> GetEditPartial(Guid id)
    {
        var user = await _sender.Send(new GetUserByIdQuery(id), HttpContext.RequestAborted);

        ViewData["Edit"] = true;

        var model = CreateUserFormModel();

        model.Id = user.Id;
        model.Email = user.Email;
        model.FirstName = user.FirstName;
        model.LastName = user.LastName;
        model.PhoneNumber = user.PhoneNumber;
        model.Role = user.Roles.FirstOrDefault() ?? string.Empty;
        model.PreferredCulture = user.PreferredCulture;
        model.PreferredTheme = user.PreferredTheme ?? SupportedThemes.System;

        model.EmailConfirmed = user.EmailConfirmed;
        model.ActivationEmailSentOn = user.ActivationEmailSentOn;
        model.ActivationEmailExpiresOn = user.ActivationEmailExpiresOn;

        model.CreatedBy = string.IsNullOrWhiteSpace(user.CreatedByName) ? _sharedLocalizer["Value.User.System"] : user.CreatedByName;
        model.CreatedOn = user.CreatedOn;
        model.UpdatedBy = string.IsNullOrWhiteSpace(user.UpdatedByName) ? _sharedLocalizer["Value.User.System"] : user.UpdatedByName;
        model.UpdatedOn = user.UpdatedOn;

        model.ActivationStatus = user.EmailConfirmed
            ? _sharedLocalizer["Message.UserAccountActivated"]
            : _sharedLocalizer["Message.UserAccountNotActivated"];

        model.ActivationEmailSentOnLabel =
            user.EmailConfirmed || user.ActivationEmailSentOn == null
                ? null
                : string.Format(
                    _sharedLocalizer["Message.EmailActivationSentOn"],
                    _dateTimeLocalizer
                        .ToLocalTime(user.ActivationEmailSentOn.Value)
                        .ToString("g", CultureInfo.CurrentUICulture));

        model.ActivationEmailExpiresOnLabel =
            user.EmailConfirmed || user.ActivationEmailExpiresOn == null
                ? null
                : string.Format(
                    _sharedLocalizer["Message.EmailActivationExpiresOn"],
                    _dateTimeLocalizer
                        .ToLocalTime(user.ActivationEmailExpiresOn.Value)
                        .ToString("g", CultureInfo.CurrentUICulture));

        return PartialView("_UserPartial", model);
    }

    [HttpGet]
    public async Task<IActionResult> GetDatatable(DataTablesParameters param, bool? isActivated = null)
    {
        var sorts = param.SortingCols?
            .Where(x => x.Column is >= 0 and <= 2 || x.Column == 4)
            .Select(x => new SortCriterion<UserSortField>(
                x.Column switch
                {
                    0 => UserSortField.LastName,
                    1 => UserSortField.FirstName,
                    2 => UserSortField.Email,
                    4 => UserSortField.IsActivated,
                    _ => throw new UnreachableException()
                },
                string.Equals(x.Dir, "desc", StringComparison.OrdinalIgnoreCase)
                    ? SortDirection.Descending
                    : SortDirection.Ascending))
            .ToList();

        var users = await _sender.Send(
            new SearchUsersQuery(
                new PagedSearchCriteria(
                    isActivated,
                    param.Search,
                    param.DisplayStart,
                    param.DisplayLength,
                    sorts)),
            HttpContext.RequestAborted);

        var culture = CultureInfo.CurrentUICulture;
        var data = users.Results
            .Select(user => new[]
            {
                string.Empty,          // Ajout de la colonne responsive
                user.LastName,
                user.FirstName,
                user.Email,
                string.Join(", ", user.Roles.Select(x => _sharedLocalizer[$"Role.{x}"])),
                user.IsActivated ? "1" : $"0¤{(user.ActivationEmailSentOn == null ? "" : string.Format(_sharedLocalizer["Message.EmailActivationSentOn"], _dateTimeLocalizer.ToLocalTime(user.ActivationEmailSentOn.Value).ToString("g", culture)))}¤{(user.ActivationEmailExpiresOn == null ? "" : string.Format(_sharedLocalizer["Message.EmailActivationExpiresOn"], _dateTimeLocalizer.ToLocalTime(user.ActivationEmailExpiresOn.Value).ToString("g", culture)))}",
                $"{user.Id}¤{(user.IsActivated ? "1" : "0")}"  // Ajout de la colonne d'actions
            })
            .ToArray();

        return Json(new
        {
            sEcho = param.Echo,
            iTotalRecords = users.TotalCount,
            iTotalDisplayRecords = users.FilteredCount,
            aaData = data
        });
    }

    [HttpPost]
    public async Task<JsonResult> PostAdd(UserFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return await Send(
            new CreateUserCommand(
                model.Email,
                model.LastName,
                model.FirstName,
                model.PhoneNumber,
                [model.Role],
                null,
                model.PreferredCulture,
                model.NormalizedPreferredTheme,
                "/Account/ActivateAccount",
                new { area = "Identity" },
                _options.Value.AccountActivationLifetime, 
                _sharedLocalizer["Email.ActivateAccount.Subject"].Value),
            MapUserFormPropertyName);
    }

    [HttpPost]
    public async Task<JsonResult> PostEdit(UserFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return await Send(
            new UpdateUserCommand(
                model.Id ?? Guid.Empty,
                model.LastName,
                model.FirstName,
                model.PhoneNumber,
                [model.Role],
                model.PreferredCulture,
                model.NormalizedPreferredTheme));
    }

    [HttpPost]
    public async Task<JsonResult> Delete(Guid id)
    {
        return await Send(new DeleteUserCommand(id));
    }

    [HttpPost]
    public async Task<JsonResult> SendActivationEmail(Guid id)
    {
        return await Send(
            new SendUserActivationEmailCommand(
                id, 
                "/Account/ActivateAccount", 
                new { area = "Identity" }, 
                _options.Value.AccountActivationLifetime, 
                _sharedLocalizer["Email.ActivateAccount.Subject"].Value));
    }


    private UserFormModel CreateUserFormModel()
    {
        return new UserFormModel
        {
            Roles =
            [
                .. ApplicationRoles.All.ToSelectListItemsList(
                x => _sharedLocalizer[$"Role.{x}"],
                x => x)
            ],
            Cultures =
            [
                .. SupportedCultures.AllWithKeys.ToSelectListItemsList(
                x => _sharedLocalizer[$"Culture.{x.Key}"],
                x => x.Value)
            ],
            Themes =
            [
                .. SupportedThemes.AllWithKeys.ToSelectListItemsList(
                x => _sharedLocalizer[$"Theme.{x.Key}"],
                x => x.Value)
            ]
        };
    }

    private static string MapUserFormPropertyName(string propertyName)
    {
        return propertyName switch
        {
            nameof(CreateUserCommand.NormalizedEmail) => nameof(UserFormModel.Email),
            nameof(CreateUserCommand.PhoneNumberE164) => nameof(UserFormModel.PhoneNumber),
            _ => propertyName
        };
    }
}

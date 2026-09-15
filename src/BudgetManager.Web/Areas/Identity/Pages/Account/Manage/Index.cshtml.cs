using BudgetManager.Application.Common;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.User.Common;
using BudgetManager.Application.Features.User.DeleteProfilePicture;
using BudgetManager.Application.Features.User.GetCurrent;
using BudgetManager.Application.Features.User.UpdateProfile;
using BudgetManager.Application.Features.User.UpdateProfilePicture;
using BudgetManager.Application.Features.User.UpdateUiPreferences;
using BudgetManager.Web.Extensions;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace BudgetManager.Web.Areas.Identity.Pages.Account.Manage;

public class IndexModel(
    ISender sender,
    IStringLocalizer<SharedResource> localizer,
    IBusinessErrorLocalizer businessErrorLocalizer) : PageModel
{
    private readonly ISender _sender = sender;
    private readonly IStringLocalizer<SharedResource> _localizer = localizer;
    private readonly IBusinessErrorLocalizer _businessErrorLocalizer = businessErrorLocalizer;

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool SynchronizeUiPreferences { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? ProfilePicture { get; set; }

    public byte[]? ProfilePictureContent { get; private set; }
    public string? ProfilePictureContentType { get; private set; }

    public sealed class InputModel()
    {
        [Required(ErrorMessage = "Validation.Required")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.Required")]
        public string LastName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Validation.Required")]
        public string PreferredCulture { get; set; } = SupportedCultures.Default;

        public string? PreferredTheme { get; set; }

        public List<SelectListItem> Cultures { get; set; } = [];

        public List<SelectListItem> Themes { get; set; } = [];
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync(forceInput: true);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var currentUser = await GetCurrentUserAsync();

        if (!ModelState.IsValid)
        {
            ApplyCurrentUser(currentUser, forceInput: false);
            return Page();
        }

        var normalizedInputPhoneNumber = PhoneNumberNormalizer.Normalize(Input.PhoneNumber);
        var profileChanged =
            !string.Equals(Input.FirstName, currentUser.FirstName, StringComparison.Ordinal) ||
            !string.Equals(Input.LastName, currentUser.LastName, StringComparison.Ordinal) ||
            !string.Equals(normalizedInputPhoneNumber, currentUser.PhoneNumber, StringComparison.Ordinal);

        var preferredTheme = NormalizePreferredTheme(Input.PreferredTheme);
        var preferencesChanged =
            !string.Equals(Input.PreferredCulture, currentUser.PreferredCulture, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(preferredTheme, currentUser.PreferredTheme, StringComparison.OrdinalIgnoreCase);

        try
        {
            if (profileChanged)
            {
                await _sender.Send(
                    new UpdateUserProfileCommand(
                        currentUser.Id,
                        Input.LastName,
                        Input.FirstName,
                        Input.PhoneNumber,
                        Input.PreferredCulture,
                        preferredTheme),
                    HttpContext.RequestAborted);
            }
            else if (preferencesChanged)
            {
                await _sender.Send(
                    new UpdateUiPreferencesCommand(
                        currentUser.Id,
                        Input.PreferredCulture,
                        preferredTheme),
                    HttpContext.RequestAborted);
            }

            if (preferencesChanged)
            {
                Response.SetRequestCultureCookie(Input.PreferredCulture);
                SynchronizeUiPreferences = true;
            }

            StatusMessage = _localizer["Message.ProfileUpdated"].Value;
            return RedirectToPage();
        }
        catch (BadRequestException exception)
        {
            AddValidationErrors(exception);
            ApplyCurrentUser(currentUser, forceInput: false);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdatePictureAsync()
    {
        var currentUser = await GetCurrentUserAsync();

        if (ProfilePicture is null || ProfilePicture.Length == 0)
        {
            ModelState.Clear();
            ModelState.AddModelError(nameof(ProfilePicture), _localizer["Validation.ProfilePictureRequired"].Value);
            ApplyCurrentUser(currentUser, forceInput: true);
            return Page();
        }

        try
        {
            await using var stream = new MemoryStream();
            await ProfilePicture.CopyToAsync(stream, HttpContext.RequestAborted);

            await _sender.Send(
                new UpdateUserProfilePictureCommand(
                    currentUser.Id,
                    new UserProfilePicture(stream.ToArray(), ProfilePicture.ContentType)),
                HttpContext.RequestAborted);

            StatusMessage = _localizer["Message.ProfilePictureUpdated"].Value;
            return RedirectToPage();
        }
        catch (BadRequestException exception)
        {
            ModelState.Clear();
            foreach (var error in exception.ValidationErrors)
            {
                ModelState.AddModelError(nameof(ProfilePicture), _businessErrorLocalizer.Localize(error));
            }

            ApplyCurrentUser(currentUser, forceInput: true);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeletePictureAsync()
    {
        var currentUser = await GetCurrentUserAsync();

        try
        {
            await _sender.Send(
                new DeleteUserProfilePictureCommand(currentUser.Id),
                HttpContext.RequestAborted);

            StatusMessage = _localizer["Message.ProfilePictureDeleted"].Value;
            return RedirectToPage();
        }
        catch (BadRequestException exception)
        {
            ModelState.Clear();
            foreach (var error in exception.ValidationErrors)
            {
                ModelState.AddModelError(nameof(ProfilePicture), _businessErrorLocalizer.Localize(error));
            }

            ApplyCurrentUser(currentUser, forceInput: true);
            return Page();
        }
    }

    private async Task LoadAsync(bool forceInput)
    {
        var currentUser = await GetCurrentUserAsync();
        ApplyCurrentUser(currentUser, forceInput);
    }

    private async Task<CurrentUserDto> GetCurrentUserAsync()
    {
        return await _sender.Send(new GetCurrentUserQuery(), HttpContext.RequestAborted);
    }

    private void ApplyCurrentUser(CurrentUserDto currentUser, bool forceInput)
    {
        ProfilePictureContent = currentUser.ProfilePictureContent;
        ProfilePictureContentType = currentUser.ProfilePictureContentType;

        if (forceInput)
        {
            Input = new InputModel
            {
                FirstName = currentUser.FirstName,
                LastName = currentUser.LastName,
                PhoneNumber = currentUser.PhoneNumber,
                PreferredCulture = currentUser.PreferredCulture,
                PreferredTheme = currentUser.PreferredTheme ?? SupportedThemes.System
            };
        }

        PopulateInputLists();
    }

    private static string? NormalizePreferredTheme(string? preferredTheme)
    {
        return string.Equals(preferredTheme, SupportedThemes.System, StringComparison.OrdinalIgnoreCase)
            ? null
            : preferredTheme;
    }

    private void AddValidationErrors(BadRequestException exception)
    {
        foreach (var error in exception.ValidationErrors)
        {
            var propertyName = error.PropertyName switch
            {
                nameof(UpdateUserProfileCommand.FirstName) => nameof(Input.FirstName),
                nameof(UpdateUserProfileCommand.LastName) => nameof(Input.LastName),
                nameof(UpdateUserProfileCommand.PhoneNumberE164) => nameof(Input.PhoneNumber),
                nameof(UpdateUserProfileCommand.PreferredCulture) => nameof(Input.PreferredCulture),
                nameof(UpdateUserProfileCommand.PreferredTheme) => nameof(Input.PreferredTheme),
                _ => string.Empty
            };

            ModelState.AddModelError(propertyName, _businessErrorLocalizer.Localize(error));
        }
    }

    private void PopulateInputLists()
    {
        Input.Cultures =
            [..
            SupportedCultures.AllWithKeys.ToSelectListItemsList(
                x => _localizer[$"Culture.{x.Key}"],
                x => x.Value)
            ];

        Input.Themes =
            [..
            SupportedThemes.AllWithKeys.ToSelectListItemsList(
                x => _localizer[$"Theme.{x.Key}"],
                x => x.Value)
            ];
    }
}

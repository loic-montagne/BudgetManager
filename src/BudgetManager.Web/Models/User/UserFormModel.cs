using BudgetManager.Application.Common;
using BudgetManager.Web.Models.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BudgetManager.Web.Models.User;

public sealed class UserFormModel : AuditableFormModel
{
    public Guid? Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = string.Empty;

    public string PreferredCulture { get; set; } = SupportedCultures.Default;

    public string PreferredTheme { get; set; } = SupportedThemes.System;

    public string? NormalizedPreferredTheme =>
        string.Equals(
            PreferredTheme,
            SupportedThemes.System,
            StringComparison.OrdinalIgnoreCase)
            ? null
            : PreferredTheme;

    public bool EmailConfirmed { get; set; }

    public DateTimeOffset? ActivationEmailSentOn { get; set; }

    public DateTimeOffset? ActivationEmailExpiresOn { get; set; }

    public string ActivationStatus { get; set; } = string.Empty;

    public string? ActivationEmailSentOnLabel { get; set; }

    public string? ActivationEmailExpiresOnLabel { get; set; }

    public List<SelectListItem> Roles { get; set; } = [];

    public List<SelectListItem> Cultures { get; set; } = [];

    public List<SelectListItem> Themes { get; set; } = [];
}

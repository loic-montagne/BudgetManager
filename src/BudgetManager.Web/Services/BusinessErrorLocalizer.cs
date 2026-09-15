using BudgetManager.Application.Common.Errors;
using Microsoft.Extensions.Localization;

namespace BudgetManager.Web.Services;

public sealed class BusinessErrorLocalizer : IBusinessErrorLocalizer
{
    private readonly IStringLocalizer<BusinessErrors> _localizer;

    public BusinessErrorLocalizer(IStringLocalizer<BusinessErrors> localizer)
    {
        _localizer = localizer;
    }

    public string Localize(ValidationError error)
    {
        if (string.IsNullOrWhiteSpace(error.ErrorCode))
        {
            return error.ErrorMessage;
        }

        var localized = _localizer[error.ErrorCode];
        return localized.ResourceNotFound ? error.ErrorMessage : localized.Value;
    }
}

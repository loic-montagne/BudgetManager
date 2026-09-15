using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;

namespace BudgetManager.Application.Features.User.UpdateUiPreferences;

public sealed record UpdateUiPreferencesCommand(Guid Id, string PreferredCulture, string? PreferredTheme) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => ApplicationRoles.All;
}

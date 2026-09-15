using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;

namespace BudgetManager.Application.Features.User.ChangeEmail;

public sealed record ChangeEmailUserCommand(Guid Id, string OldEmail, string NewEmail) : ICommand<string>, IAuthorizedRequest
{
    public string NormalizedOldEmail => OldEmail?.Trim() ?? string.Empty;
    public string NormalizedNewEmail => NewEmail?.Trim() ?? string.Empty;
    public IReadOnlyCollection<string> RequiredRoles => ApplicationRoles.All;
}

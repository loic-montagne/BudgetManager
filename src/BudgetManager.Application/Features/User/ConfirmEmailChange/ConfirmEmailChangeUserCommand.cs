using BudgetManager.Application.Abstractions.Messaging;

namespace BudgetManager.Application.Features.User.ConfirmEmailChange;

public sealed record ConfirmEmailChangeUserCommand(Guid Id, string Email, string Token) : ICommand
{
    public string NormalizedEmail => Email?.Trim() ?? string.Empty;
}

using BudgetManager.Application.Abstractions.Messaging;

namespace BudgetManager.Application.Features.User.Activate;

public sealed record ActivateUserCommand(Guid Id, string ActivationToken, string Password, string ConfirmPassword) : ICommand;

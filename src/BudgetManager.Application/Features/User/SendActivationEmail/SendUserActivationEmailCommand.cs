using BudgetManager.Application.Abstractions.Messaging;

namespace BudgetManager.Application.Features.User.SendActivationEmail;

public sealed record SendUserActivationEmailCommand(Guid Id, string ActivationPageName, object? RouteValues, TimeSpan TokenLifetime, string Subject) : ICommand<DateTimeOffset>;

namespace BudgetManager.Application.Features.User.Search;

public sealed record UserDto(Guid Id, string Email, string LastName, string FirstName, bool IsActivated, DateTimeOffset? ActivationEmailSentOn, DateTimeOffset? ActivationEmailExpiresOn, IReadOnlyCollection<string> Roles);

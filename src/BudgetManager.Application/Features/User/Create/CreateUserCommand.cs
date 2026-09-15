using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;
using BudgetManager.Application.Features.User.Common;

namespace BudgetManager.Application.Features.User.Create;

public sealed record CreateUserCommand(string Email, string LastName, string FirstName, string? PhoneNumber, IReadOnlyCollection<string> Roles, UserProfilePicture? ProfilePicture, string PreferredCulture, string? PreferredTheme, string ActivationPageName, object? RouteValues, TimeSpan ActivationTokenLifetime, string ActivationEmailSubject) : ICommand<(Guid id, DateTimeOffset expiresOn)>, IAuthorizedRequest
{
    public string NormalizedEmail => Email?.Trim() ?? string.Empty;
    public string? PhoneNumberE164 => PhoneNumberNormalizer.Normalize(PhoneNumber);
    public IReadOnlyCollection<string> RequiredRoles => [ApplicationRoles.Administrator];

    public UserProfileData ToUserData()
    {
        return new UserProfileData(
            LastName,
            FirstName,
            PhoneNumberE164,
            PreferredCulture,
            PreferredTheme);
    }
}

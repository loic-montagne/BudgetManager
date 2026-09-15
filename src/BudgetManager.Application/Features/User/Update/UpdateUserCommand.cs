using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;
using BudgetManager.Application.Features.User.Common;

namespace BudgetManager.Application.Features.User.Update;

public sealed record UpdateUserCommand(Guid Id, string LastName, string FirstName, string? PhoneNumber, IReadOnlyCollection<string> Roles, string PreferredCulture, string? PreferredTheme) : ICommand, IAuthorizedRequest
{
    public string? PhoneNumberE164 => PhoneNumberNormalizer.Normalize(PhoneNumber);
    public IReadOnlyCollection<string> RequiredRoles => [ ApplicationRoles.Administrator ];

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

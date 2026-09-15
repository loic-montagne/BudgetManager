using BudgetManager.Application.Common;

namespace BudgetManager.Application.Features.User.Common;

public sealed record UserProfileData(string LastName, string FirstName, string? PhoneNumberE164, string PreferredCulture, string? PreferredTheme)
{
}

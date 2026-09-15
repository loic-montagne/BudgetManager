using BudgetManager.Application.Features.User.Common;

namespace BudgetManager.Application.Features.User.GetById;

public sealed record UserDto(Guid Id, string UserName, string LastName, string FirstName, string Email, bool EmailConfirmed, string PreferredCulture, string? PreferredTheme, byte[]? ProfilePictureContent, string? ProfilePictureContentType)
{
    public UserProfileData ToUserData()
    {
        return new UserProfileData(
            LastName,
            FirstName,
            null,
            PreferredCulture,
            PreferredTheme);
    }
}

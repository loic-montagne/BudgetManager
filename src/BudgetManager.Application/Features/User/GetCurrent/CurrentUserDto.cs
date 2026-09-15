namespace BudgetManager.Application.Features.User.GetCurrent;

public sealed record CurrentUserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    IReadOnlyCollection<string> Roles,
    string PreferredCulture,
    string? PreferredTheme,
    byte[]? ProfilePictureContent,
    string? ProfilePictureContentType);

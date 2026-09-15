using BudgetManager.Application.Features.User.Common;

namespace BudgetManager.Application.Abstractions.Identity;

public interface IUserManager
{
    Task<bool> RoleExistsAsync(string role, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ProfilePictureExistsAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> EmailIsCurrentAsync(Guid id, string email, CancellationToken cancellationToken);
    Task<bool> IsBudgetOwnerAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsLastActivatedAdministratorAsync(Guid id, CancellationToken cancellationToken);

    Task<Guid> CreateAsync(string email, UserProfileData user, UserProfilePicture? profilePicture, string password, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, UserProfileData user, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task ActivateAsync(Guid id, string activationToken, string password, CancellationToken cancellationToken);
    Task SaveActivationEmailAsync(Guid id, DateTimeOffset? sentOn, DateTimeOffset? expiresOn, CancellationToken cancellationToken);

    Task<string> ChangeEmailAsync(Guid id, string email, CancellationToken cancellationToken);
    Task ConfirmEmailChangeAsync(Guid id, string email, string token, CancellationToken cancellationToken);
    Task UpdateProfileAsync(Guid id, UserProfileData user, CancellationToken cancellationToken);
    Task UpdateUiPreferencesAsync(Guid id, string preferredCulture, string? preferredTheme, CancellationToken cancellationToken);
    Task UpdateProfilePictureAsync(Guid userId, UserProfilePicture picture, CancellationToken cancellationToken);
    Task DeleteProfilePictureAsync(Guid userId, CancellationToken cancellationToken);
}

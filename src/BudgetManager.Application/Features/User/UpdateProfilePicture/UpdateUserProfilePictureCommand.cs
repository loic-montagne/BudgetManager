using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;
using BudgetManager.Application.Features.User.Common;

namespace BudgetManager.Application.Features.User.UpdateProfilePicture;

public sealed record UpdateUserProfilePictureCommand(Guid UserId, UserProfilePicture ProfilePicture) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => ApplicationRoles.All;
}

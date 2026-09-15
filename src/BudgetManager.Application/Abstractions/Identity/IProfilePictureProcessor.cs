using BudgetManager.Application.Common;
using BudgetManager.Application.Features.User.Common;

namespace BudgetManager.Application.Abstractions.Identity;

public interface IProfilePictureProcessor
{
    Result<UserProfilePicture> Process(UserProfilePicture picture, CancellationToken cancellationToken);
}

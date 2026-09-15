using BudgetManager.Application.Common;
using BudgetManager.Application.Features.User.Common;

namespace BudgetManager.Application.Abstractions.Identity;

public interface IPasswordValidator
{
    Task<Result> ValidateAsync(string email, UserProfileData data, string? password, CancellationToken cancellationToken);
}

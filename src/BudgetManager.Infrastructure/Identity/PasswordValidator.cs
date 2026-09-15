using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common;
using BudgetManager.Application.Features.User.Common;
using BudgetManager.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;

namespace BudgetManager.Infrastructure.Identity;

internal sealed class PasswordValidator(UserManager<ApplicationUser> userManager, IPasswordValidator<ApplicationUser> passwordValidator) : IPasswordValidator
{
    public async Task<Result> ValidateAsync(string email, UserProfileData data, string? password, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = data.FirstName,
            LastName = data.LastName,
            PhoneNumber = data.PhoneNumberE164,
            PreferredCulture = data.PreferredCulture,
            PreferredTheme = data.PreferredTheme,
        };
        var result = await passwordValidator.ValidateAsync(userManager, user, password);
        return result.GetResult();
    }
}

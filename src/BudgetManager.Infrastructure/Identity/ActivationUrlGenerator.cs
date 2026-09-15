using BudgetManager.Application.Abstractions.Api;
using BudgetManager.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace BudgetManager.Infrastructure.Identity;

internal sealed class ActivationUrlGenerator(UserManager<ApplicationUser> userManager, IApplicationUrlBuilder applicationUrlBuilder) : IActivationUrlGenerator
{
    public async Task<string> Generate(Guid userId, string activationPageName, object? routeValues, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NullReferenceException("User cannot be null.");

        var activationCode = await userManager.GenerateEmailConfirmationTokenAsync(user);
        activationCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(activationCode));

        var values = new RouteValueDictionary(routeValues)
        {
            ["activationCode"] = activationCode
        };

        return applicationUrlBuilder.GetPageUrl(activationPageName, values);
    }
}

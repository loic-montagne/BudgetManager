using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Features.User.UpdateUiPreferences;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Web.Controllers;

[Authorize]
public sealed class UiPreferencesController(ISender sender, ICurrentUser currentUser) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromBody] UpdateUiPreferencesRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateUiPreferencesCommand(
                currentUser.RequiredUserId,
                request.PreferredCulture,
                request.PreferredTheme),
            cancellationToken);

        return NoContent();
    }

    public sealed record UpdateUiPreferencesRequest(string PreferredCulture, string? PreferredTheme);
}

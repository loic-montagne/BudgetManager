using BudgetManager.Application.Abstractions.Identity;
using MediatR;

namespace BudgetManager.Application.Features.User.UpdateUiPreferences;

public sealed class UpdateUiPreferencesCommandHandler(IUserManager userManager) : IRequestHandler<UpdateUiPreferencesCommand>
{
    public async Task Handle(UpdateUiPreferencesCommand request, CancellationToken cancellationToken)
    {
        await userManager.UpdateUiPreferencesAsync(request.Id, request.PreferredCulture, request.PreferredTheme, cancellationToken);
    }
}

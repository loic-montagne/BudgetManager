namespace BudgetManager.Application.Abstractions.Identity;

public interface IActivationUrlGenerator
{
    Task<string> Generate(Guid userId, string activationPageName, object? routeValues, CancellationToken cancellationToken);
}

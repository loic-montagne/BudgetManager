namespace BudgetManager.Application.Abstractions.Api;

public interface IApplicationUrlBuilder
{
    string GetPageUrl(string pageName, object? values = null);
}

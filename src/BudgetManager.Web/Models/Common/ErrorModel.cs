namespace BudgetManager.Web.Models.Common;

public sealed record ErrorModel(string? RequestId, int StatusCode)
{
    public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
}

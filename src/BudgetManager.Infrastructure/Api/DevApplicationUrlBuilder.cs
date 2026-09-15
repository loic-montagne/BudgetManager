using BudgetManager.Application.Abstractions.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BudgetManager.Infrastructure.Api;

public sealed class DevApplicationUrlBuilder(LinkGenerator linkGenerator, IHttpContextAccessor httpContextAccessor) : IApplicationUrlBuilder
{
    public string GetPageUrl(string pageName, object? values = null)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("An active HTTP context is required.");
        var url = linkGenerator.GetUriByPage(httpContext, page: pageName, values: values, scheme: httpContext.Request.Scheme);
        return url
            ?? throw new InvalidOperationException($"Unable to generate URL for page '{pageName}'.");
    }
}

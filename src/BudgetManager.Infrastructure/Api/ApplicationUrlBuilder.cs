using BudgetManager.Application.Abstractions.Api;
using BudgetManager.Infrastructure.Configuration;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace BudgetManager.Infrastructure.Api;

internal sealed class ApplicationUrlBuilder(LinkGenerator linkGenerator, IOptions<ApplicationOptions> options) : IApplicationUrlBuilder
{
    private readonly Uri _publicUrl = EnsureTrailingSlash(options.Value.PublicUrl);

    public string GetPageUrl(string pageName, object? values = null)
    {
        var path = linkGenerator.GetPathByPage(page: pageName, values: values)
            ?? throw new InvalidOperationException($"Unable to generate URL for page '{pageName}'.");
        return new Uri(_publicUrl, path.TrimStart('/')).AbsoluteUri;
    }

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        if (!uri.IsAbsoluteUri)
            throw new InvalidOperationException("Application:PublicUrl must be an absolute URI.");

        return uri.AbsoluteUri.EndsWith('/')
            ? uri : new Uri($"{uri.AbsoluteUri}/");
    }
}


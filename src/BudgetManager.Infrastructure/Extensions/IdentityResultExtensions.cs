using BudgetManager.Application.Common;
using BudgetManager.Application.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace BudgetManager.Infrastructure.Extensions;

internal static class IdentityResultExtensions
{
    public static Result GetResult(this IdentityResult? result)
    {
        if (result == null || result.Succeeded)
            return new Result([]);
        return new Result(result.Errors.Select(x => (x.Code, x.Description)).ToList());
    }

    public static void EnsureSucceeded(this IdentityResult? result, string errMessage)
    {
        if (result == null || result.Succeeded)
            return;

        var errors = string.Join(Environment.NewLine, result.Errors.Select(x => $"{x.Code}: {x.Description}"));

        throw new UpdateException("One or more errors occurred while saving user.", new InvalidOperationException($"{errMessage}:{Environment.NewLine}{errors}"));
    }
}

namespace BudgetManager.Application.Common;

public sealed record Result(IReadOnlyList<(string Code, string Message)> Errors)
{
    public bool IsValid => !Errors.Any();
}

public sealed record Result<T>(T Value, IReadOnlyList<(string Code, string Message)> Errors)
    where T : class
{
    public bool IsValid => !Errors.Any();
}

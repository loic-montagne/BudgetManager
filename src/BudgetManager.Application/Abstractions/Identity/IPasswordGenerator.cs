namespace BudgetManager.Application.Abstractions.Identity;

public interface IPasswordGenerator
{
    string Generate(int length);
}

using BudgetManager.Application.Email;
using System.Globalization;

namespace BudgetManager.Application.Abstractions.Email;

public interface ITemplatedEmailSender
{
    Task SendAsync<TModel>(TemplatedEmailMessage message, TModel model, CultureInfo culture, CancellationToken cancellationToken);
}

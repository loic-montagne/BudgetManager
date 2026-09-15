using BudgetManager.Application.Email;
using System.Globalization;

namespace BudgetManager.Application.Abstractions.Email;

public interface IEmailTemplateRenderer
{
    Task<RenderedEmailTemplate> RenderAsync<TModel>(string templateName, TModel model, CultureInfo culture, CancellationToken cancellationToken);
}
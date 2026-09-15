using BudgetManager.Application.Abstractions.Email;
using BudgetManager.Application.Common;
using BudgetManager.Application.Email;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BudgetManager.Infrastructure.Email;

internal sealed partial class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private const string TemplateNamespace = "BudgetManager.Infrastructure.Email.Templates";

    public async Task<RenderedEmailTemplate> RenderAsync<TModel>(string templateName, TModel model, CultureInfo culture, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentNullException.ThrowIfNull(model);

        var htmlTemplate = await LoadTemplateAsync(templateName, culture, "html", cancellationToken);
        var textTemplate = await LoadTemplateAsync(templateName, culture, "txt", cancellationToken);

        var htmlBody = htmlTemplate is not null ? Render(htmlTemplate, model, true) : null;
        var textBody = textTemplate is not null ? Render(textTemplate, model, false) : null;

        if (htmlBody is null && textBody is null)
            throw new InvalidOperationException($"Email template '{templateName}' was not found.");

        return new RenderedEmailTemplate(textBody, htmlBody);
    }

    private static async Task<string?> LoadTemplateAsync(string templateName, CultureInfo culture, string extension, CancellationToken cancellationToken)
    {
        var assembly = typeof(EmailTemplateRenderer).Assembly;

        var resourceName = $"{TemplateNamespace}.{templateName}.{culture.Name}.{extension}";

        var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream is null && !culture.Name.Equals(SupportedCultures.Default, StringComparison.OrdinalIgnoreCase))
        {
            resourceName = $"{TemplateNamespace}.{templateName}.{SupportedCultures.Default}.{extension}";
            stream = assembly.GetManifestResourceStream(resourceName);
        }

        if (stream is null)
            return null;

        await using (stream)
        using (var reader = new StreamReader(stream))
        {
            return await reader.ReadToEndAsync(cancellationToken);
        }
    }

    private static string Render<TModel>(string template, TModel model, bool encodeHtml)
    {
        var values =
            typeof(TModel)
                .GetProperties(
                    BindingFlags.Instance |
                    BindingFlags.Public)
                .ToDictionary(
                    property => property.Name,
                    property =>
                        property.GetValue(model));

        return PlaceholderRegex()
            .Replace(
                template,
                match =>
                {
                    var propertyName = match.Groups["name"].Value;

                    if (!values.TryGetValue(propertyName, out var value))
                    {
                        throw new InvalidOperationException($"Property '{propertyName}' does not exist on email template model '{typeof(TModel).Name}'.");
                    }

                    var text = Convert.ToString(value, CultureInfo.InvariantCulture)
                        ?? string.Empty;

                    return encodeHtml
                        ? HtmlEncoder.EncodeWithLineBreaks(text)
                        : text;
                });
    }

    [GeneratedRegex(@"\{\{\s*(?<name>[A-Za-z0-9_]+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();
}

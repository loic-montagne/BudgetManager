using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BudgetManager.Web.TagHelpers;

[HtmlTargetElement("label", Attributes = ForAttributeName)]
public sealed class RequiredLabelTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-for";

    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression For { get; set; } = null!;

    public override int Order => 1000;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!For.Metadata.IsRequired)
        {
            return;
        }

        if (output.Attributes.TryGetAttribute("class", out var classAttribute) &&
            classAttribute.Value?.ToString()
                                ?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                 .Contains("form-check-label") == true)
        {
            return;
        }

        output.PostContent.AppendHtml(
            "<span class=\"required-indicator\" aria-hidden=\"true\">*</span>");
    }
}

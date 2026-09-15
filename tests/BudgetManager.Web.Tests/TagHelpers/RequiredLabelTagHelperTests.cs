using BudgetManager.Web.TagHelpers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace BudgetManager.Web.Tests.TagHelpers;

public sealed class RequiredLabelTagHelperTests
{
    private sealed class Model
    {
        [Required]
        public string Required { get; set; } = string.Empty;

        public string? Optional { get; set; }
    }

    [Fact]
    public void Process_WithRequiredProperty_AppendsIndicator()
    {
        var helper = Create(nameof(Model.Required));
        var output = CreateOutput();
        helper.Process(CreateContext(), output);
        Assert.Contains("required-indicator", output.PostContent.GetContent());
    }

    [Fact]
    public void Process_WithOptionalProperty_DoesNotAppendIndicator()
    {
        var helper = Create(nameof(Model.Optional));
        var output = CreateOutput();
        helper.Process(CreateContext(), output);
        Assert.Equal(string.Empty, output.PostContent.GetContent());
    }

    [Fact]
    public void Process_WithFormCheckLabel_DoesNotAppendIndicator()
    {
        var helper = Create(nameof(Model.Required));
        var output = CreateOutput();
        output.Attributes.SetAttribute("class", "form-check-label other");
        helper.Process(CreateContext(), output);
        Assert.Equal(string.Empty, output.PostContent.GetContent());
    }

    [Fact]
    public void Order_IsAfterDefaultLabelTagHelper()
    {
        var helper = Create(nameof(Model.Required));

        Assert.Equal(1000, helper.Order);
    }

    [Fact]
    public void Process_WithRequiredPropertyAndUnrelatedClasses_AppendsIndicator()
    {
        var helper = Create(nameof(Model.Required));
        var output = CreateOutput();
        output.Attributes.SetAttribute("class", "form-label other");

        helper.Process(CreateContext(), output);

        Assert.Contains("required-indicator", output.PostContent.GetContent());
    }

    private static RequiredLabelTagHelper Create(string property)
    {
        var services = new ServiceCollection();
        services.AddMvc();

        using var serviceProvider = services.BuildServiceProvider();

        var provider = serviceProvider.GetRequiredService<IModelMetadataProvider>();
        var explorer = provider
            .GetModelExplorerForType(typeof(Model), null)
            .GetExplorerForProperty(property);

        return new RequiredLabelTagHelper { For = new ModelExpression(property, explorer) };
    }
    private static TagHelperContext CreateContext() => new(new TagHelperAttributeList(), new Dictionary<object, object>(), "id");
    private static TagHelperOutput CreateOutput() => new("label", new TagHelperAttributeList(), (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
}

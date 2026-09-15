using BudgetManager.Web;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Web.Services;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Services;

public sealed class BusinessErrorLocalizerTests
{
    [Fact]
    public void Localize_WithoutErrorCode_ReturnsOriginalMessage()
    {
        var localizer = Substitute.For<IStringLocalizer<BusinessErrors>>();
        var service = new BusinessErrorLocalizer(localizer);
        Assert.Equal("message", service.Localize(new ValidationError("Field", "message", null)));
    }

    [Fact]
    public void Localize_WithKnownResource_ReturnsLocalizedValue()
    {
        var localizer = Substitute.For<IStringLocalizer<BusinessErrors>>();
        localizer["Code"].Returns(new LocalizedString("Code", "localized", false));
        var service = new BusinessErrorLocalizer(localizer);
        Assert.Equal("localized", service.Localize(new ValidationError("Field", "fallback", "Code")));
    }

    [Fact]
    public void Localize_WithMissingResource_ReturnsOriginalMessage()
    {
        var localizer = Substitute.For<IStringLocalizer<BusinessErrors>>();
        localizer["Code"].Returns(new LocalizedString("Code", "Code", true));
        var service = new BusinessErrorLocalizer(localizer);
        Assert.Equal("fallback", service.Localize(new ValidationError("Field", "fallback", "Code")));
    }
}

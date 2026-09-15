using BudgetManager.Application.Common;
using BudgetManager.Web.Models.Account;
using BudgetManager.Web.Models.Bank;
using BudgetManager.Web.Models.BudgetCategory;
using BudgetManager.Web.Models.Common;
using BudgetManager.Web.Models.Datatables;
using BudgetManager.Web.Models.User;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BudgetManager.Web.Tests.Models;

public sealed class WebModelsTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("id", true)]
    public void ErrorModel_ShowRequestId_ReflectsRequestId(string? requestId, bool expected)
        => Assert.Equal(expected, new ErrorModel(requestId, 500).ShowRequestId);

    [Fact]
    public void UserFormModel_NormalizedPreferredTheme_MapsSystemToNull()
    {
        Assert.Null(new UserFormModel { PreferredTheme = SupportedThemes.System }.NormalizedPreferredTheme);
        Assert.Equal("dark", new UserFormModel { PreferredTheme = "dark" }.NormalizedPreferredTheme);
    }

    [Fact]
    public void FormModels_Defaults_AreSafeForBinding()
    {
        Assert.NotNull(new AccountFormModel().Banks);
        Assert.Equal(string.Empty, new BankFormModel().Name);
        Assert.Equal(string.Empty, new BudgetCategoryFormModel().Name);
        Assert.NotNull(new UserFormModel().Roles);
    }

    [Fact]
    public void DataTablesParameters_HasExpectedModelBinder()
    {
        var attribute = Assert.Single(typeof(DataTablesParameters).GetCustomAttributes(typeof(ModelBinderAttribute), true).Cast<ModelBinderAttribute>());
        Assert.Equal(typeof(DataTablesModelBinder), attribute.BinderType);
    }
}

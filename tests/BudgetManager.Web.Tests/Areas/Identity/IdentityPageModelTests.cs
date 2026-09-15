using BudgetManager.Web.Areas.Identity.Pages.Account;
using BudgetManager.Web.Areas.Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Xunit;

namespace BudgetManager.Web.Tests.Areas.Identity;

public sealed class IdentityPageModelTests
{
    [Fact]
    public void ConfirmationAndLockoutPages_OnGet_AreNoOpAndConstructible()
    {
        new ActivateAccountConfirmation().OnGet();
        new ForgotPasswordConfirmation().OnGet();
        new LockoutModel().OnGet();
        new ResetPasswordConfirmationModel().OnGet();
    }

    [Fact]
    public void ManageNavPages_PageNavClass_MatchesCurrentPage()
    {
        var viewContext = new ViewContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary()) };
        viewContext.ViewData["ActivePage"] = ManageNavPages.Email;
        Assert.Equal("active", ManageNavPages.EmailNavClass(viewContext));
        Assert.Null(ManageNavPages.IndexNavClass(viewContext));
    }

    [Fact]
    public void InputModels_ExposeExpectedValidationMetadata()
    {
        Assert.Contains(typeof(ActivateAccountModel.InputModel).GetProperty(nameof(ActivateAccountModel.InputModel.Email))!.GetCustomAttributes(true), x => x is System.ComponentModel.DataAnnotations.RequiredAttribute);
        Assert.Contains(typeof(LoginModel.InputModel).GetProperty(nameof(LoginModel.InputModel.Email))!.GetCustomAttributes(true), x => x is System.ComponentModel.DataAnnotations.EmailAddressAttribute);
        Assert.Contains(typeof(ResetPasswordModel.InputModel).GetProperty(nameof(ResetPasswordModel.InputModel.Password))!.GetCustomAttributes(true), x => x is System.ComponentModel.DataAnnotations.StringLengthAttribute);
        Assert.Contains(typeof(ChangePasswordModel.InputModel).GetProperty(nameof(ChangePasswordModel.InputModel.NewPassword))!.GetCustomAttributes(true), x => x is System.ComponentModel.DataAnnotations.StringLengthAttribute);
    }
}

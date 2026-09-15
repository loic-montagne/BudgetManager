using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Features.User.UpdateUiPreferences;
using BudgetManager.Web.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Controllers;

public sealed class UiPreferencesControllerTests
{
    [Fact]
    public async Task Update_SendsCommandForCurrentUserAndReturnsNoContent()
    {
        var sender = Substitute.For<ISender>();
        var currentUser = Substitute.For<ICurrentUser>();
        var id = Guid.NewGuid();
        currentUser.RequiredUserId.Returns(id);
        var controller = new UiPreferencesController(sender, currentUser);
        var token = TestContext.Current.CancellationToken;
        var result = await controller.Update(new("fr-FR", "dark"), token);
        Assert.IsType<NoContentResult>(result);
        await sender.Received(1).Send(Arg.Is<UpdateUiPreferencesCommand>(x => x.Id == id && x.PreferredCulture == "fr-FR" && x.PreferredTheme == "dark"), token);
    }
}

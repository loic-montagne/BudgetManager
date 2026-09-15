using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Exceptions.Common;
using BudgetManager.Web.Controllers;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Controllers;

public sealed class SenderControllerTests
{
    private sealed class TestApplicationException(string message)
    : BudgetManager.Application.Exceptions.Common.ApplicationException(message);

    [Fact]
    public async Task Send_WithResponseCommand_ReturnsSuccessfulResult()
    {
        var command = Substitute.For<ICommand<int>>();
        var sender = Substitute.For<ISender>();
        sender.Send(command, TestContext.Current.CancellationToken).Returns(42);

        dynamic result = await SenderController.Send(
            command,
            null,
            sender,
            Localizer(),
            Errors(),
            TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(42, result.Result);
    }

    [Fact]
    public async Task Send_WithCommand_ReturnsSuccessfulResult()
    {
        var command = Substitute.For<ICommand>();
        var sender = Substitute.For<ISender>();

        dynamic result = await SenderController.Send(
            command,
            null,
            sender,
            Localizer(),
            Errors(),
            TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        await sender.Received(1).Send(command, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Send_WithValidationErrors_MapsFieldAndGeneralErrors()
    {
        var command = Substitute.For<ICommand>();
        var sender = Substitute.For<ISender>();
        var errors = Errors();
        var fieldError = new ValidationError("Email", "invalid email", "User.Email.Invalid");
        var generalError = new ValidationError(string.Empty, "general error", "General.Error");

        errors.Localize(fieldError).Returns("Localized field");
        errors.Localize(generalError).Returns("Localized general");

        sender.Send(command, TestContext.Current.CancellationToken)
            .Returns(_ => throw new BadRequestException("invalid", [fieldError, generalError]));

        dynamic result = await SenderController.Send(
            command,
            name => $"Input.{name}",
            sender,
            Localizer(),
            errors,
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(result.InvalidControls);
        Assert.Equal("Input.Email", result.InvalidControls[0].Name);
        Assert.Equal("Localized field", result.InvalidControls[0].Text);
        Assert.Equal("Localized general", result.Error);
    }

    [Fact]
    public async Task Send_WithBudgetManagerApplicationException_ReturnsExceptionMessage()
    {
        var command = Substitute.For<ICommand>();
        var sender = Substitute.For<ISender>();
        sender.Send(command, TestContext.Current.CancellationToken)
            .Returns(_ => throw new TestApplicationException("application error"));

        dynamic result = await SenderController.Send(
            command,
            null,
            sender,
            Localizer(),
            Errors(),
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("application error", result.Error);
    }

    [Fact]
    public async Task Send_WithUnhandledException_ReturnsLocalizedGenericError()
    {
        var command = Substitute.For<ICommand>();
        var sender = Substitute.For<ISender>();
        var localizer = Localizer();
        localizer["Error.NotManagedGeneric"].Returns(new LocalizedString("Error.NotManagedGeneric", "Unexpected: {0}"));
        sender.Send(command, TestContext.Current.CancellationToken)
            .Returns(_ => throw new InvalidOperationException("boom"));

        dynamic result = await SenderController.Send(
            command,
            null,
            sender,
            localizer,
            Errors(),
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Unexpected: boom", result.Error);
    }

    private static IStringLocalizer<SharedResource> Localizer()
        => Substitute.For<IStringLocalizer<SharedResource>>();

    private static IBusinessErrorLocalizer Errors()
        => Substitute.For<IBusinessErrorLocalizer>();
}

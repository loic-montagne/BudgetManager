using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Email;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Abstractions.Localization;
using BudgetManager.Application.Common;
using BudgetManager.Application.Email;
using BudgetManager.Application.Email.Templates;
using BudgetManager.Application.Features.User.Activate;
using BudgetManager.Application.Features.User.GetById;
using BudgetManager.Application.Features.User.SendActivationEmail;
using NSubstitute;
using System.Globalization;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class UserActivationCommandHandlerTests
{
    [Fact]
    public async Task ActivateUserHandler_WhenValid_DelegatesActivationToUserManager()
    {
        // Arrange

        var userManager = Substitute.For<IUserManager>();
        var id = Guid.NewGuid();
        var handler = new ActivateUserCommandHandler(userManager);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        await handler.Handle(
            new ActivateUserCommand(
                id,
                "activation-token",
                "NewPassword!123",
                "NewPassword!123"),
            cancellationToken);

        // Assert

        await userManager
            .Received(1)
            .ActivateAsync(
                id,
                "activation-token",
                "NewPassword!123",
                cancellationToken);
    }

    [Fact]
    public async Task SendActivationEmailHandler_WhenValid_UsesLocalExpirationAndPersistsUtcValidity()
    {
        // Arrange

        var id = Guid.NewGuid();
        var sentOn = new DateTimeOffset(2026, 9, 8, 15, 13, 0, TimeSpan.Zero);
        var expiresOn = sentOn.AddDays(7);
        var localExpiresOn = expiresOn.ToOffset(TimeSpan.FromHours(2));
        var culture = CultureInfo.GetCultureInfo(SupportedCultures.French);

        var user = CreateUserDto(id);
        var userContext = Substitute.For<IUserContext>();
        userContext
            .GetRequiredAsync(id, Arg.Any<CancellationToken>())
            .Returns(user);

        var userManager = Substitute.For<IUserManager>();
        var activationUrlGenerator = Substitute.For<IActivationUrlGenerator>();
        activationUrlGenerator
            .Generate(
                id,
                "/Account/ActivateAccount",
                Arg.Any<object?>(),
                Arg.Any<CancellationToken>())
            .Returns("https://example.test/activate?activationCode=encoded-token");

        var emailSender = Substitute.For<ITemplatedEmailSender>();
        var dateTimeLocalizer = Substitute.For<IDateTimeLocalizer>();
        dateTimeLocalizer
            .ToLocalTime(expiresOn)
            .Returns(localExpiresOn);

        var handler = new SendUserActivationEmailCommandHandler(
            userContext,
            userManager,
            activationUrlGenerator,
            emailSender,
            dateTimeLocalizer,
            new FixedTimeProvider(sentOn));

        var cancellationToken = TestContext.Current.CancellationToken;
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;

        try
        {
            // Act

            await handler.Handle(
                new SendUserActivationEmailCommand(
                    id,
                    "/Account/ActivateAccount",
                    new { area = "Identity" },
                    TimeSpan.FromDays(7),
                    "Activate your account"),
                cancellationToken);

            // Assert

            await emailSender
                .Received(1)
                .SendAsync(
                    Arg.Is<TemplatedEmailMessage>(message =>
                        message.Subject == "Activate your account"
                        && message.TemplateName == EmailTemplates.AccountActivation
                        && message.To.Count == 1
                        && message.To[0].Address == "user@example.test"),
                    Arg.Is<AccountActivationEmailModel>(model =>
                        model.FirstName == "First"
                        && model.ActivationUrl == "https://example.test/activate?activationCode=encoded-token"
                        && model.ExpiresOn == localExpiresOn.ToString("g", culture)),
                    Arg.Is<CultureInfo>(x => x.Name == culture.Name),
                    cancellationToken);

            await activationUrlGenerator
                .Received(1)
                .Generate(
                    id,
                    "/Account/ActivateAccount",
                    Arg.Any<object?>(),
                    cancellationToken);

            dateTimeLocalizer
                .Received(1)
                .ToLocalTime(expiresOn);

            await userManager
                .Received(1)
                .SaveActivationEmailAsync(
                    id,
                    sentOn,
                    expiresOn,
                    cancellationToken);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public async Task SendActivationEmailHandler_WhenEmailSendingFails_DoesNotPersistActivationValidity()
    {
        // Arrange

        var id = Guid.NewGuid();
        var sentOn = new DateTimeOffset(2026, 9, 8, 15, 13, 0, TimeSpan.Zero);
        var userContext = Substitute.For<IUserContext>();
        userContext
            .GetRequiredAsync(id, Arg.Any<CancellationToken>())
            .Returns(CreateUserDto(id));

        var userManager = Substitute.For<IUserManager>();
        var activationUrlGenerator = Substitute.For<IActivationUrlGenerator>();
        activationUrlGenerator
            .Generate(
                id,
                Arg.Any<string>(),
                Arg.Any<object?>(),
                Arg.Any<CancellationToken>())
            .Returns("https://example.test/activate?activationCode=encoded-token");

        var emailSender = Substitute.For<ITemplatedEmailSender>();
        var dateTimeLocalizer = Substitute.For<IDateTimeLocalizer>();
        dateTimeLocalizer
            .ToLocalTime(Arg.Any<DateTimeOffset>())
            .Returns(callInfo => callInfo.Arg<DateTimeOffset>());

        var cancellationToken = TestContext.Current.CancellationToken;
        emailSender
            .SendAsync(
                Arg.Any<TemplatedEmailMessage>(),
                Arg.Any<AccountActivationEmailModel>(),
                Arg.Any<CultureInfo>(),
                cancellationToken)
            .Returns(Task.FromException(new InvalidOperationException("SMTP failure.")));

        var handler = new SendUserActivationEmailCommandHandler(
            userContext,
            userManager,
            activationUrlGenerator,
            emailSender,
            dateTimeLocalizer,
            new FixedTimeProvider(sentOn));

        // Act

        var action = () => handler.Handle(
            new SendUserActivationEmailCommand(
                id,
                "/Account/ActivateAccount",
                new { area = "Identity" },
                TimeSpan.FromDays(7),
                "Activate your account"),
            cancellationToken);

        // Assert

        await Assert.ThrowsAsync<InvalidOperationException>(action);

        await userManager
            .DidNotReceiveWithAnyArgs()
            .SaveActivationEmailAsync(
                default,
                default,
                default,
                cancellationToken);
    }

    [Fact]
    public async Task SendActivationEmailHandler_WhenActivationUrlGenerationFails_DoesNotSendOrPersistActivationValidity()
    {
        // Arrange

        var id = Guid.NewGuid();
        var sentOn = new DateTimeOffset(2026, 9, 8, 15, 13, 0, TimeSpan.Zero);
        var userContext = Substitute.For<IUserContext>();
        userContext
            .GetRequiredAsync(id, Arg.Any<CancellationToken>())
            .Returns(CreateUserDto(id));

        var userManager = Substitute.For<IUserManager>();
        var activationUrlGenerator = Substitute.For<IActivationUrlGenerator>();
        var emailSender = Substitute.For<ITemplatedEmailSender>();
        var dateTimeLocalizer = Substitute.For<IDateTimeLocalizer>();
        var cancellationToken = TestContext.Current.CancellationToken;

        activationUrlGenerator
            .Generate(
                id,
                "/Account/ActivateAccount",
                Arg.Any<object?>(),
                cancellationToken)
            .Returns(Task.FromException<string>(new InvalidOperationException("URL generation failure.")));

        var handler = new SendUserActivationEmailCommandHandler(
            userContext,
            userManager,
            activationUrlGenerator,
            emailSender,
            dateTimeLocalizer,
            new FixedTimeProvider(sentOn));

        // Act

        var action = () => handler.Handle(
            new SendUserActivationEmailCommand(
                id,
                "/Account/ActivateAccount",
                new { area = "Identity" },
                TimeSpan.FromDays(7),
                "Activate your account"),
            cancellationToken);

        // Assert

        await Assert.ThrowsAsync<InvalidOperationException>(action);

        await emailSender
            .DidNotReceiveWithAnyArgs()
            .SendAsync<AccountActivationEmailModel>(
                default!,
                default!,
                default!,
                cancellationToken);

        await userManager
            .DidNotReceiveWithAnyArgs()
            .SaveActivationEmailAsync(
                default,
                default,
                default,
                cancellationToken);
    }

    private static UserDto CreateUserDto(Guid id)
        => new(
            id,
            "user@example.test",
            "Last",
            "First",
            "user@example.test",
            false,
            SupportedCultures.French,
            null,
            null,
            null);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

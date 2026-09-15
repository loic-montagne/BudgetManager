using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Features.User.Activate;
using BudgetManager.Application.Features.User.Common;
using UserGetByIdDto = BudgetManager.Application.Features.User.GetById.UserDto;
using BudgetManager.Application.Features.User.SendActivationEmail;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class UserActivationValidatorTests
{
    [Fact]
    public async Task ActivateUserValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var id = Guid.NewGuid();
        var userContext = PendingUserContext(id);
        var passwordValidator = ValidPasswordValidator();
        var validator = new ActivateUserCommandValidator(userContext, passwordValidator);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var result = await validator.TestValidateAsync(
            ValidActivateCommand(id),
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();

        await passwordValidator
            .Received(1)
            .ValidateAsync(
                "user@example.test",
                Arg.Any<UserProfileData>(),
                "ValidPassword!123",
                cancellationToken);
    }

    [Fact]
    public async Task ActivateUserValidator_WhenIdIsEmpty_ReturnsRequired()
    {
        // Arrange

        var userContext = UserContext(Guid.Empty, emailConfirmed: false);
        var passwordValidator = ValidPasswordValidator();
        var validator = new ActivateUserCommandValidator(userContext, passwordValidator);

        // Act

        var result = await validator.TestValidateAsync(
            ValidActivateCommand(Guid.Empty),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.UserIdRequired);

        await passwordValidator
            .DidNotReceiveWithAnyArgs()
            .ValidateAsync(default!, default!, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ActivateUserValidator_WhenUserDoesNotExist_ReturnsNotExists()
    {
        // Arrange

        var id = Guid.NewGuid();
        var userContext = Substitute.For<IUserContext>();
        userContext
            .ExistsAsync(id, Arg.Any<CancellationToken>())
            .Returns(false);
        userContext
            .GetAsync(id, Arg.Any<CancellationToken>())
            .Returns((UserGetByIdDto?)null);

        var passwordValidator = ValidPasswordValidator();
        var validator = new ActivateUserCommandValidator(userContext, passwordValidator);

        // Act

        var result = await validator.TestValidateAsync(
            ValidActivateCommand(id),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.UserNotExists);

        await passwordValidator
            .DidNotReceiveWithAnyArgs()
            .ValidateAsync(default!, default!, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ActivateUserValidator_WhenActivationTokenIsEmpty_ReturnsRequired()
    {
        // Arrange

        var id = Guid.NewGuid();
        var validator = new ActivateUserCommandValidator(PendingUserContext(id), ValidPasswordValidator());

        // Act

        var result = await validator.TestValidateAsync(
            ValidActivateCommand(id) with { ActivationToken = string.Empty },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.ActivationToken)
            .WithErrorCode(ErrorCodes.UserActivationTokenRequired);
    }

    [Fact]
    public async Task ActivateUserValidator_WhenPasswordIsEmpty_ReturnsRequiredAndSkipsIdentityValidation()
    {
        // Arrange

        var id = Guid.NewGuid();
        var passwordValidator = Substitute.For<IPasswordValidator>();
        var validator = new ActivateUserCommandValidator(PendingUserContext(id), passwordValidator);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var result = await validator.TestValidateAsync(
            ValidActivateCommand(id) with { Password = string.Empty, ConfirmPassword = string.Empty },
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode(ErrorCodes.UserPasswordRequired);

        await passwordValidator
            .DidNotReceiveWithAnyArgs()
            .ValidateAsync(default!, default!, default, cancellationToken);
    }

    [Fact]
    public async Task ActivateUserValidator_WhenPasswordsDoNotMatch_ReturnsExpectedError()
    {
        // Arrange

        var id = Guid.NewGuid();
        var validator = new ActivateUserCommandValidator(PendingUserContext(id), ValidPasswordValidator());

        // Act

        var result = await validator.TestValidateAsync(
            ValidActivateCommand(id) with { ConfirmPassword = "DifferentPassword!123" },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode(ErrorCodes.UserPasswordNotEqualToConfirm);
    }

    [Fact]
    public async Task ActivateUserValidator_WhenPasswordPolicyFails_ReturnsAllIdentityErrors()
    {
        // Arrange

        var id = Guid.NewGuid();
        var passwordValidator = Substitute.For<IPasswordValidator>();
        passwordValidator
            .ValidateAsync(
                Arg.Any<string>(),
                Arg.Any<UserProfileData>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new Result(
                [
                    ("PasswordTooShort", "Password is too short."),
                    ("PasswordRequiresDigit", "Password requires a digit.")
                ]));

        var validator = new ActivateUserCommandValidator(PendingUserContext(id), passwordValidator);

        // Act

        var result = await validator.TestValidateAsync(
            ValidActivateCommand(id) with { Password = "weak", ConfirmPassword = "weak" },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            2,
            result.Errors.Count(x =>
                x.PropertyName == nameof(ActivateUserCommand.Password)
                && x.ErrorCode == ErrorCodes.UserPasswordInvalid));
    }

    [Fact]
    public async Task ActivateUserValidator_WhenEmailIsAlreadyConfirmed_ReturnsExpectedError()
    {
        // Arrange

        var id = Guid.NewGuid();
        var validator = new ActivateUserCommandValidator(
            UserContext(id, emailConfirmed: true),
            ValidPasswordValidator());

        // Act

        var result = await validator.TestValidateAsync(
            ValidActivateCommand(id),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.UserEmailAlreadyConfirmed);
    }

    [Fact]
    public async Task SendActivationEmailValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var id = Guid.NewGuid();
        var validator = new SendUserActivationEmailCommandValidator(PendingUserContext(id));

        // Act

        var result = await validator.TestValidateAsync(
            ValidSendActivationCommand(id),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task SendActivationEmailValidator_WhenIdIsEmpty_ReturnsRequiredAndStopsUserLookup()
    {
        // Arrange

        var userContext = Substitute.For<IUserContext>();
        var validator = new SendUserActivationEmailCommandValidator(userContext);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var result = await validator.TestValidateAsync(
            ValidSendActivationCommand(Guid.Empty),
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.UserIdRequired);

        await userContext
            .DidNotReceiveWithAnyArgs()
            .GetRequiredAsync(default, cancellationToken);
    }

    [Fact]
    public async Task SendActivationEmailValidator_WhenUserDoesNotExist_ReturnsNotExistsAndStopsUserLookup()
    {
        // Arrange

        var id = Guid.NewGuid();
        var userContext = Substitute.For<IUserContext>();
        userContext
            .ExistsAsync(id, Arg.Any<CancellationToken>())
            .Returns(false);

        var validator = new SendUserActivationEmailCommandValidator(userContext);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var result = await validator.TestValidateAsync(
            ValidSendActivationCommand(id),
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.UserNotExists);

        await userContext
            .DidNotReceiveWithAnyArgs()
            .GetRequiredAsync(default, cancellationToken);
    }

    [Theory]
    [InlineData("", "Activate your account", ErrorCodes.UserActivationPageNameRequired)]
    [InlineData("/Account/ActivateAccount", "", ErrorCodes.UserActivationEmailSubjectRequired)]
    public async Task SendActivationEmailValidator_WhenRequiredDataIsMissing_ReturnsExpectedError(
        string activationPageName,
        string subject,
        string errorCode)
    {
        // Arrange

        var id = Guid.NewGuid();
        var validator = new SendUserActivationEmailCommandValidator(PendingUserContext(id));

        // Act

        var result = await validator.TestValidateAsync(
            new SendUserActivationEmailCommand(
                id,
                activationPageName,
                new { area = "Identity" },
                TimeSpan.FromDays(7),
                subject),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert

        Assert.Contains(result.Errors, x => x.ErrorCode == errorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SendActivationEmailValidator_WhenTokenLifetimeIsNotPositive_ReturnsInvalid(
        int lifetimeTicks)
    {
        // Arrange

        var id = Guid.NewGuid();
        var validator = new SendUserActivationEmailCommandValidator(PendingUserContext(id));

        // Act

        var result = await validator.TestValidateAsync(
            ValidSendActivationCommand(id) with
            {
                TokenLifetime = TimeSpan.FromTicks(lifetimeTicks)
            },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.TokenLifetime)
            .WithErrorCode(ErrorCodes.UserActivationTokenLifetimeInvalid);
    }

    [Fact]
    public async Task SendActivationEmailValidator_WhenEmailIsAlreadyConfirmed_ReturnsExpectedError()
    {
        // Arrange

        var id = Guid.NewGuid();
        var validator = new SendUserActivationEmailCommandValidator(
            UserContext(id, emailConfirmed: true));

        // Act

        var result = await validator.TestValidateAsync(
            ValidSendActivationCommand(id),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.UserEmailAlreadyConfirmed);
    }

    private static ActivateUserCommand ValidActivateCommand(Guid id)
        => new(
            id,
            "activation-token",
            "ValidPassword!123",
            "ValidPassword!123");

    private static SendUserActivationEmailCommand ValidSendActivationCommand(Guid id)
        => new(
            id,
            "/Account/ActivateAccount",
            new { area = "Identity" },
            TimeSpan.FromDays(7),
            "Activate your account");

    private static IUserContext PendingUserContext(Guid id)
        => UserContext(id, emailConfirmed: false);

    private static IUserContext UserContext(Guid id, bool emailConfirmed)
    {
        var userContext = Substitute.For<IUserContext>();
        var user = new UserGetByIdDto(
            id,
            "user@example.test",
            "Last",
            "First",
            "user@example.test",
            emailConfirmed,
            SupportedCultures.French,
            null,
            null,
            null);

        userContext
            .ExistsAsync(id, Arg.Any<CancellationToken>())
            .Returns(true);

        userContext
            .GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(user);

        userContext
            .GetRequiredAsync(id, Arg.Any<CancellationToken>())
            .Returns(user);

        return userContext;
    }

    private static IPasswordValidator ValidPasswordValidator()
    {
        var passwordValidator = Substitute.For<IPasswordValidator>();
        passwordValidator
            .ValidateAsync(
                Arg.Any<string>(),
                Arg.Any<UserProfileData>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new Result([]));

        return passwordValidator;
    }
}

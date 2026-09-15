using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.User.ChangeEmail;
using BudgetManager.Application.Features.User.ConfirmEmailChange;
using BudgetManager.Application.Features.User.Common;
using BudgetManager.Application.Features.User.Create;
using BudgetManager.Application.Features.User.Delete;
using BudgetManager.Application.Features.User.DeleteProfilePicture;
using BudgetManager.Application.Features.User.SendActivationEmail;
using BudgetManager.Application.Features.User.Update;
using BudgetManager.Application.Features.User.UpdateProfile;
using BudgetManager.Application.Features.User.UpdateProfilePicture;
using BudgetManager.Application.Features.User.UpdateUiPreferences;
using MediatR;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class UserManagementCommandHandlerTests
{
    [Fact]
    public async Task CreateUserHandler_WhenNoPicture_PassesNormalizedProfilePreferencesAndNullPicture()
    {
        // Arrange

        var manager =
            Substitute.For<IUserManager>();

        var passwordGenerator =
            Substitute.For<IPasswordGenerator>();

        passwordGenerator
            .Generate(20)
            .Returns("TemporaryPassword!1");

        var processor =
            Substitute.For<IProfilePictureProcessor>();

        var sender =
            Substitute.For<ISender>();

        var expectedId =
            Guid.NewGuid();

        var expectedExpiresOn =
            new DateTimeOffset(2026, 9, 18, 15, 13, 0, TimeSpan.Zero);

        UserProfileData? capturedProfile =
            null;

        UserProfilePicture? capturedPicture =
            new([9], "image/test");

        manager
            .CreateAsync(
                "admin@example.test",
                Arg.Do<UserProfileData>(x => capturedProfile = x),
                Arg.Do<UserProfilePicture?>(x => capturedPicture = x),
                "TemporaryPassword!1",
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedId);

        var command =
            new CreateUserCommand(
                "  admin@example.test ",
                "Last",
                "First",
                "06 12 34 56 78",
                [ApplicationRoles.Administrator],
                null,
                SupportedCultures.English,
                SupportedThemes.Dark,
                "/Account/ActivateAccount",
                new { area = "Identity" },
                TimeSpan.FromDays(7),
                "Activate your account");

        sender
            .Send(
                Arg.Is<SendUserActivationEmailCommand>(x =>
                    x.Id == expectedId
                    && x.ActivationPageName == "/Account/ActivateAccount"
                    && x.TokenLifetime == TimeSpan.FromDays(7)
                    && x.Subject == "Activate your account"),
                Arg.Any<CancellationToken>())
            .Returns(expectedExpiresOn);

        var handler =
            new CreateUserCommandHandler(
                sender,
                manager,
                passwordGenerator,
                processor);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await handler.Handle(
                command,
                cancellationToken);

        // Assert

        Assert.Equal(
            expectedId,
            result.id);

        Assert.Equal(
            expectedExpiresOn,
            result.expiresOn);

        Assert.NotNull(
            capturedProfile);

        Assert.Equal(
            "Last",
            capturedProfile.LastName);

        Assert.Equal(
            "First",
            capturedProfile.FirstName);

        Assert.Equal(
            "+33612345678",
            capturedProfile.PhoneNumberE164);

        Assert.Equal(
            SupportedCultures.English,
            capturedProfile.PreferredCulture);

        Assert.Equal(
            SupportedThemes.Dark,
            capturedProfile.PreferredTheme);

        Assert.Null(
            capturedPicture);

        passwordGenerator
            .Received(1)
            .Generate(20);

        processor
            .DidNotReceiveWithAnyArgs()
            .Process(
                default!,
                cancellationToken);
    }

    [Fact]
    public async Task CreateUserHandler_WhenPictureProvided_PassesProcessedPicture()
    {
        // Arrange

        var manager =
            Substitute.For<IUserManager>();

        var passwordGenerator =
            Substitute.For<IPasswordGenerator>();

        passwordGenerator
            .Generate(20)
            .Returns("TemporaryPassword!1");

        var processor =
            Substitute.For<IProfilePictureProcessor>();

        var original =
            new UserProfilePicture(
                [1, 2, 3],
                SupportedPictureFormats.PngContentType);

        var processed =
            new UserProfilePicture(
                [4, 5, 6],
                SupportedPictureFormats.WebpContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        processor
            .Process(
                original,
                cancellationToken)
            .Returns(
                new Result<UserProfilePicture>(
                    processed,
                    []));

        var sender =
            Substitute.For<ISender>();

        var id =
            Guid.NewGuid();

        manager
            .CreateAsync(
                Arg.Any<string>(),
                Arg.Any<UserProfileData>(),
                processed,
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(id);

        sender
            .Send(
                Arg.Any<SendUserActivationEmailCommand>(),
                cancellationToken)
            .Returns(DateTimeOffset.UtcNow);

        var command =
            new CreateUserCommand(
                "admin@example.test",
                "Last",
                "First",
                null,
                [ApplicationRoles.Administrator],
                original,
                SupportedCultures.French,
                null,
                "/Account/ActivateAccount",
                new { area = "Identity" },
                TimeSpan.FromDays(7),
                "Activate your account");

        var handler =
            new CreateUserCommandHandler(
                sender,
                manager,
                passwordGenerator,
                processor);

        // Act

        await handler.Handle(
            command,
            cancellationToken);

        // Assert

        processor
            .Received(1)
            .Process(
                original,
                cancellationToken);

        await manager
            .Received(1)
            .CreateAsync(
                "admin@example.test",
                Arg.Any<UserProfileData>(),
                processed,
                "TemporaryPassword!1",
                Arg.Any<IReadOnlyCollection<string>>(),
                cancellationToken);
    }

    [Fact]
    public async Task CreateUserHandler_WhenPictureProcessingFails_ThrowsAndDoesNotCreateUser()
    {
        // Arrange

        var manager =
            Substitute.For<IUserManager>();

        var passwordGenerator =
            Substitute.For<IPasswordGenerator>();

        var processor =
            Substitute.For<IProfilePictureProcessor>();

        var sender =
            Substitute.For<ISender>();

        var picture =
            new UserProfilePicture(
                [1],
                SupportedPictureFormats.PngContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        processor
            .Process(
                picture,
                cancellationToken)
            .Returns(
                new Result<UserProfilePicture>(
                    picture,
                    [
                        (
                            ErrorCodes.UserProfilePictureInvalid,
                            "Invalid profile picture.")
                    ]));

        var command =
            new CreateUserCommand(
                "admin@example.test",
                "Last",
                "First",
                null,
                [ApplicationRoles.Administrator],
                picture,
                SupportedCultures.French,
                null,
                "/Account/ActivateAccount",
                new { area = "Identity" },
                TimeSpan.FromDays(7),
                "Activate your account");

        var handler =
            new CreateUserCommandHandler(
                sender,
                manager,
                passwordGenerator,
                processor);

        // Act

        var action = () =>
            handler.Handle(
                command,
                cancellationToken);

        // Assert

        var exception =
            await Assert.ThrowsAsync<BadRequestException>(
                action);

        Assert.Contains(
            exception.ValidationErrors,
            x =>
                x.PropertyName == nameof(CreateUserCommand.ProfilePicture)
                && x.ErrorCode == ErrorCodes.UserProfilePictureInvalid);

        await manager
            .DidNotReceiveWithAnyArgs()
            .CreateAsync(
                default!,
                default!,
                default,
                default!,
                default!,
                cancellationToken);

        passwordGenerator
            .DidNotReceiveWithAnyArgs()
            .Generate(default);

        await sender
            .DidNotReceiveWithAnyArgs()
            .Send(
                Arg.Any<SendUserActivationEmailCommand>(),
                cancellationToken);
    }

    [Fact]
    public async Task CreateUserHandler_WhenActivationEmailSendingFails_PropagatesFailureAfterUserCreation()
    {
        // Arrange

        var sender =
            Substitute.For<ISender>();

        var manager =
            Substitute.For<IUserManager>();

        var passwordGenerator =
            Substitute.For<IPasswordGenerator>();

        passwordGenerator
            .Generate(20)
            .Returns("TemporaryPassword!1");

        var processor =
            Substitute.For<IProfilePictureProcessor>();

        var id =
            Guid.NewGuid();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        manager
            .CreateAsync(
                Arg.Any<string>(),
                Arg.Any<UserProfileData>(),
                Arg.Any<UserProfilePicture?>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                cancellationToken)
            .Returns(id);

        sender
            .Send(
                Arg.Any<SendUserActivationEmailCommand>(),
                cancellationToken)
            .Returns(Task.FromException<DateTimeOffset>(
                new InvalidOperationException("SMTP failure.")));

        var handler =
            new CreateUserCommandHandler(
                sender,
                manager,
                passwordGenerator,
                processor);

        var command =
            new CreateUserCommand(
                "admin@example.test",
                "Last",
                "First",
                null,
                [ApplicationRoles.Administrator],
                null,
                SupportedCultures.French,
                null,
                "/Account/ActivateAccount",
                new { area = "Identity" },
                TimeSpan.FromDays(7),
                "Activate your account");

        // Act

        var action = () =>
            handler.Handle(
                command,
                cancellationToken);

        // Assert

        await Assert.ThrowsAsync<InvalidOperationException>(action);

        await manager
            .Received(1)
            .CreateAsync(
                "admin@example.test",
                Arg.Any<UserProfileData>(),
                null,
                "TemporaryPassword!1",
                Arg.Any<IReadOnlyCollection<string>>(),
                cancellationToken);
    }

    [Fact]
    public async Task UpdateUserHandler_PassesExpectedProfilePreferencesAndRoles()
    {
        // Arrange

        var manager =
            Substitute.For<IUserManager>();

        var id =
            Guid.NewGuid();

        UserProfileData? capturedProfile =
            null;

        IReadOnlyCollection<string>? capturedRoles =
            null;

        manager
            .UpdateAsync(
                id,
                Arg.Do<UserProfileData>(x => capturedProfile = x),
                Arg.Do<IReadOnlyCollection<string>>(x => capturedRoles = x),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var command =
            new UpdateUserCommand(
                id,
                "Last",
                "First",
                "0033612345678",
                [
                    ApplicationRoles.User,
                    ApplicationRoles.Administrator
                ],
                SupportedCultures.English,
                SupportedThemes.System);

        var handler =
            new UpdateUserCommandHandler(
                manager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        await handler.Handle(
            command,
            cancellationToken);

        // Assert

        Assert.NotNull(
            capturedProfile);

        Assert.Equal(
            "Last",
            capturedProfile.LastName);

        Assert.Equal(
            "First",
            capturedProfile.FirstName);

        Assert.Equal(
            "+33612345678",
            capturedProfile.PhoneNumberE164);

        Assert.Equal(
            SupportedCultures.English,
            capturedProfile.PreferredCulture);

        Assert.Equal(
            SupportedThemes.System,
            capturedProfile.PreferredTheme);

        Assert.Equal(
            [
                ApplicationRoles.User,
                ApplicationRoles.Administrator
            ],
            capturedRoles);

        await manager
            .Received(1)
            .UpdateAsync(
                id,
                Arg.Any<UserProfileData>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                cancellationToken);
    }

    [Fact]
    public async Task UpdateUserProfilePictureHandler_WhenProcessingSucceeds_StoresProcessedPicture()
    {
        // Arrange

        var manager =
            Substitute.For<IUserManager>();

        var processor =
            Substitute.For<IProfilePictureProcessor>();

        var id =
            Guid.NewGuid();

        var original =
            new UserProfilePicture(
                [1],
                SupportedPictureFormats.PngContentType);

        var processed =
            new UserProfilePicture(
                [2],
                SupportedPictureFormats.WebpContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        processor
            .Process(
                original,
                cancellationToken)
            .Returns(
                new Result<UserProfilePicture>(
                    processed,
                    []));

        var handler =
            new UpdateUserProfilePictureCommandHandler(
                manager,
                processor);

        // Act

        await handler.Handle(
            new UpdateUserProfilePictureCommand(
                id,
                original),
            cancellationToken);

        // Assert

        await manager
            .Received(1)
            .UpdateProfilePictureAsync(
                id,
                processed,
                cancellationToken);
    }

    [Fact]
    public async Task UpdateUserProfilePictureHandler_WhenProcessingFails_ThrowsAndDoesNotPersist()
    {
        // Arrange

        var manager =
            Substitute.For<IUserManager>();

        var processor =
            Substitute.For<IProfilePictureProcessor>();

        var id =
            Guid.NewGuid();

        var picture =
            new UserProfilePicture(
                [1],
                SupportedPictureFormats.PngContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        processor
            .Process(
                picture,
                cancellationToken)
            .Returns(
                new Result<UserProfilePicture>(
                    picture,
                    [
                        (
                            ErrorCodes.UserProfilePictureInvalid,
                            "Invalid profile picture.")
                    ]));

        var handler =
            new UpdateUserProfilePictureCommandHandler(
                manager,
                processor);

        // Act

        var action = () =>
            handler.Handle(
                new UpdateUserProfilePictureCommand(
                    id,
                    picture),
                cancellationToken);

        // Assert

        await Assert.ThrowsAsync<BadRequestException>(
            action);

        await manager
            .DidNotReceiveWithAnyArgs()
            .UpdateProfilePictureAsync(
                default,
                default!,
                cancellationToken);
    }

    [Fact]
    public async Task DeleteUserProfilePictureHandler_DelegatesToUserManager()
    {
        // Arrange

        var manager =
            Substitute.For<IUserManager>();

        var id =
            Guid.NewGuid();

        var handler =
            new DeleteUserProfilePictureCommandHandler(
                manager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        await handler.Handle(
            new DeleteUserProfilePictureCommand(
                id),
            cancellationToken);

        // Assert

        await manager
            .Received(1)
            .DeleteProfilePictureAsync(
                id,
                cancellationToken);
    }

    [Fact]
    public async Task ChangeEmailUserHandler_PassesNormalizedEmail()
    {
        // Arrange

        var manager =
            Substitute.For<IUserManager>();

        var id =
            Guid.NewGuid();

        var handler =
            new ChangeEmailUserCommandHandler(
                manager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        await handler.Handle(
            new ChangeEmailUserCommand(
                id,
                "old@example.test",
                "  New@example.test  "),
            cancellationToken);

        // Assert

        await manager
            .Received(1)
            .ChangeEmailAsync(
                id,
                "New@example.test",
                cancellationToken);
    }

    [Fact]
    public async Task ConfirmEmailChangeUserHandler_PassesNormalizedEmailAndToken()
    {
        // Arrange

        var manager =
            Substitute.For<IUserManager>();

        var id =
            Guid.NewGuid();

        var handler =
            new ConfirmEmailChangeUserCommandHandler(
                manager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        await handler.Handle(
            new ConfirmEmailChangeUserCommand(
                id,
                "  new@example.test  ",
                "token"),
            cancellationToken);

        // Assert

        await manager
            .Received(1)
            .ConfirmEmailChangeAsync(
                id,
                "new@example.test",
                "token",
                cancellationToken);
    }

    [Fact]
    public async Task UpdateUserProfileHandler_PassesNormalizedProfile()
    {
        // Arrange

        var manager =
            Substitute.For<IUserManager>();

        var id =
            Guid.NewGuid();

        UserProfileData? capturedProfile =
            null;

        manager
            .UpdateProfileAsync(
                id,
                Arg.Do<UserProfileData>(x => capturedProfile = x),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler =
            new UpdateUserProfileCommandHandler(
                manager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        await handler.Handle(
            new UpdateUserProfileCommand(
                id,
                "Last",
                "First",
                "0033612345678",
                SupportedCultures.English,
                SupportedThemes.Dark),
            cancellationToken);

        // Assert

        Assert.NotNull(
            capturedProfile);

        Assert.Equal(
            "Last",
            capturedProfile.LastName);

        Assert.Equal(
            "First",
            capturedProfile.FirstName);

        Assert.Equal(
            "+33612345678",
            capturedProfile.PhoneNumberE164);

        Assert.Equal(
            SupportedCultures.English,
            capturedProfile.PreferredCulture);

        Assert.Equal(
            SupportedThemes.Dark,
            capturedProfile.PreferredTheme);

        await manager
            .Received(1)
            .UpdateProfileAsync(
                id,
                Arg.Any<UserProfileData>(),
                cancellationToken);
    }


    [Fact]
    public async Task UpdateUiPreferencesHandler_DelegatesToUserManager()
    {
        // Arrange

        var manager =
            Substitute.For<IUserManager>();

        var id =
            Guid.NewGuid();

        var handler =
            new UpdateUiPreferencesCommandHandler(
                manager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        await handler.Handle(
            new UpdateUiPreferencesCommand(
                id,
                SupportedCultures.English,
                SupportedThemes.Dark),
            cancellationToken);

        // Assert

        await manager
            .Received(1)
            .UpdateUiPreferencesAsync(
                id,
                SupportedCultures.English,
                SupportedThemes.Dark,
                cancellationToken);
    }

    [Fact]
    public async Task DeleteUserHandler_DelegatesToUserManager()
    {
        // Arrange

        var manager =
            Substitute.For<IUserManager>();

        var id =
            Guid.NewGuid();

        var handler =
            new DeleteUserCommandHandler(
                manager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        await handler.Handle(
            new DeleteUserCommand(
                id),
            cancellationToken);

        // Assert

        await manager
            .Received(1)
            .DeleteAsync(
                id,
                cancellationToken);
    }

    [Fact]
    public void UserManagementCommands_ExposeExpectedAuthorizationRoles()
    {
        // Arrange

        var createCommand =
            new CreateUserCommand(
                "a@b.test",
                "L",
                "F",
                null,
                [ApplicationRoles.User],
                null,
                SupportedCultures.French,
                null,
                "/Account/ActivateAccount",
                new { area = "Identity" },
                TimeSpan.FromDays(7),
                "Activate your account");

        var updateCommand =
            new UpdateUserCommand(
                Guid.NewGuid(),
                "L",
                "F",
                null,
                [ApplicationRoles.User],
                SupportedCultures.French,
                null);

        var updateProfileCommand =
            new UpdateUserProfileCommand(
                Guid.NewGuid(),
                "L",
                "F",
                null,
                SupportedCultures.French,
                null);

        var updateUiPreferencesCommand =
            new UpdateUiPreferencesCommand(
                Guid.NewGuid(),
                SupportedCultures.French,
                null);

        var updatePictureCommand =
            new UpdateUserProfilePictureCommand(
                Guid.NewGuid(),
                new UserProfilePicture(
                    [1],
                    SupportedPictureFormats.PngContentType));

        var deletePictureCommand =
            new DeleteUserProfilePictureCommand(
                Guid.NewGuid());

        var changeEmailCommand =
            new ChangeEmailUserCommand(
                Guid.NewGuid(),
                "old@b.test",
                "a@b.test");

        var deleteCommand =
            new DeleteUserCommand(
                Guid.NewGuid());

        // Act / Assert

        Assert.Equal(
            [ApplicationRoles.Administrator],
            createCommand.RequiredRoles);

        Assert.Equal(
            [ApplicationRoles.Administrator],
            updateCommand.RequiredRoles);

        Assert.Equal(
            ApplicationRoles.All,
            updateProfileCommand.RequiredRoles);

        Assert.Equal(
            ApplicationRoles.All,
            updateUiPreferencesCommand.RequiredRoles);

        Assert.Equal(
            ApplicationRoles.All,
            updatePictureCommand.RequiredRoles);

        Assert.Equal(
            ApplicationRoles.All,
            deletePictureCommand.RequiredRoles);

        Assert.Equal(
            ApplicationRoles.All,
            changeEmailCommand.RequiredRoles);

        Assert.Equal(
            [ApplicationRoles.Administrator],
            deleteCommand.RequiredRoles);
    }
}

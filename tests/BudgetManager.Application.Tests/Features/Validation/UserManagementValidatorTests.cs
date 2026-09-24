using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Features.User.ChangeEmail;
using BudgetManager.Application.Features.User.ConfirmEmailChange;
using BudgetManager.Application.Features.User.Create;
using BudgetManager.Application.Features.User.Delete;
using UserGetByIdDto = BudgetManager.Application.Features.User.GetById.UserDto;
using BudgetManager.Application.Features.User.DeleteProfilePicture;
using BudgetManager.Application.Features.User.Update;
using BudgetManager.Application.Features.User.UpdateProfile;
using BudgetManager.Application.Features.User.UpdateProfilePicture;
using BudgetManager.Application.Features.User.UpdateUiPreferences;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;
using BudgetManager.Application.Features.User.Common;

namespace BudgetManager.Application.Tests;

public sealed class UserManagementValidatorTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("06 12 34 56 78", "+33612345678")]
    [InlineData("06.12.34.56.78", "+33612345678")]
    [InlineData("06-12-34-56-78", "+33612345678")]
    [InlineData("(06) 12 34 56 78", "+33612345678")]
    [InlineData("0033612345678", "+33612345678")]
    [InlineData("+33612345678", "+33612345678")]
    public void PhoneNumberNormalizer_ReturnsExpectedValue(string? input, string? expected)
        => Assert.Equal(expected, PhoneNumberNormalizer.Normalize(input));

    [Fact]
    public async Task CreateUserValidator_WhenCommandIsValid_HasNoErrors()
    {
        var userManager = ValidUserManager();
        userManager
            .EmailExistsAsync(
                Arg.Any<string>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        var validator = new CreateUserCommandValidator(
            userManager);

        var result = await validator.TestValidateAsync(
            ValidCreate(),
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task CreateUserValidator_NormalizesEmailForUniqueness()
    {
        var userManager = ValidUserManager();
        userManager
            .EmailExistsAsync(
                "admin@example.test",
                null,
                Arg.Any<CancellationToken>())
            .Returns(false);
        var validator = new CreateUserCommandValidator(
            userManager);
        var command = ValidCreate(email: "  admin@example.test  ");

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldNotHaveAnyValidationErrors();
        await userManager.Received(1).EmailExistsAsync("admin@example.test", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUserValidator_WhenEmailIsNull_ReturnsRequiredWithoutThrowing()
    {
        var validator = new CreateUserCommandValidator(
            ValidUserManager());
        var command = ValidCreate(email: null!);

        var exception = await Record.ExceptionAsync(
            () => validator.TestValidateAsync(command, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(exception);
        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.NormalizedEmail)

            .WithErrorCode(ErrorCodes.UserEmailRequired);
    }

    [Fact]
    public async Task CreateUserValidator_WhenEmailIsEmpty_ReturnsRequiredAndStopsEmailChecks()
    {
        var userManager = ValidUserManager();
        var validator = new CreateUserCommandValidator(
            userManager);

        var result = await validator.TestValidateAsync(
            ValidCreate(email: "   "),
            cancellationToken: TestContext.Current.CancellationToken);

        result


            .ShouldHaveValidationErrorFor(x => x.NormalizedEmail)


            .WithErrorCode(ErrorCodes.UserEmailRequired);
        await userManager
            .DidNotReceiveWithAnyArgs()
            .EmailExistsAsync(
                default!,
                default,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateUserValidator_WhenEmailIsTooLong_ReturnsExpectedError()
    {
        var validator = new CreateUserCommandValidator(
            ValidUserManager());
        var email = new string('a', BudgetManager.Domain.Common.StringPropertyLengths.EmailLength + 1);
        var result = await validator.TestValidateAsync(
            ValidCreate(email: email),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.NormalizedEmail)

            .WithErrorCode(ErrorCodes.UserEmailTooLong);
    }

    [Fact]
    public async Task CreateUserValidator_WhenEmailFormatIsInvalid_ReturnsExpectedError()
    {
        var validator = new CreateUserCommandValidator(
            ValidUserManager());
        var result = await validator.TestValidateAsync(
            ValidCreate(email: "not-an-email"),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.NormalizedEmail)

            .WithErrorCode(ErrorCodes.UserEmailInvalid);
    }

    [Fact]
    public async Task CreateUserValidator_WhenEmailAlreadyExists_ReturnsExpectedError()
    {
        var userManager = ValidUserManager();
        userManager
            .EmailExistsAsync(
                "admin@example.test",
                null,
                Arg.Any<CancellationToken>())
            .Returns(true);
        var validator = new CreateUserCommandValidator(
            userManager);
        var result = await validator.TestValidateAsync(
            ValidCreate(),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.NormalizedEmail)

            .WithErrorCode(ErrorCodes.UserEmailAlreadyUsed);
    }

    [Fact]
    public async Task CreateUserValidator_WhenNamesAreMissing_ReturnsExpectedErrors()
    {
        var validator = new CreateUserCommandValidator(
            ValidUserManager());
        var result = await validator.TestValidateAsync(
            ValidCreate(lastName: "", firstName: ""),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.LastName)

            .WithErrorCode(ErrorCodes.UserLastNameRequired);
        result

            .ShouldHaveValidationErrorFor(x => x.FirstName)

            .WithErrorCode(ErrorCodes.UserFirstNameRequired);
    }

    [Fact]
    public async Task CreateUserValidator_WhenNamesAreTooLong_ReturnsExpectedErrors()
    {
        var validator = new CreateUserCommandValidator(
            ValidUserManager());
        var result = await validator.TestValidateAsync(
            ValidCreate(
                lastName: new string('x', BudgetManager.Domain.Common.StringPropertyLengths.LastNameLength + 1),
                firstName: new string('x', BudgetManager.Domain.Common.StringPropertyLengths.FirstNameLength + 1)),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.LastName)

            .WithErrorCode(ErrorCodes.UserLastNameTooLong);
        result

            .ShouldHaveValidationErrorFor(x => x.FirstName)

            .WithErrorCode(ErrorCodes.UserFirstNameTooLong);
    }

    [Fact]
    public async Task CreateUserValidator_WhenPhoneIsMissing_HasNoPhoneError()
    {
        var validator = new CreateUserCommandValidator(
            ValidUserManager());
        var result = await validator.TestValidateAsync(
            ValidCreate(phone: null),
            cancellationToken: TestContext.Current.CancellationToken);
        result.ShouldNotHaveValidationErrorFor(

            x => x.PhoneNumberE164);
    }

    [Theory]
    [InlineData("12345", ErrorCodes.UserPhoneNumberInvalid)]
    [InlineData("+1234567890123456", ErrorCodes.UserPhoneNumberTooLong)]
    public async Task CreateUserValidator_WhenPhoneIsInvalid_ReturnsExpectedError(string phone, string errorCode)
    {
        var validator = new CreateUserCommandValidator(
            ValidUserManager());
        var result = await validator.TestValidateAsync(
            ValidCreate(phone: phone),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(
            result.Errors,
            x =>
                x.PropertyName == nameof(CreateUserCommand.PhoneNumberE164)
                && x.ErrorCode == errorCode);
    }

    [Fact]
    public async Task CreateUserValidator_WhenRolesAreEmpty_ReturnsRequired()
    {
        var validator = new CreateUserCommandValidator(
            ValidUserManager());
        var result = await validator.TestValidateAsync(
            ValidCreate(roles: []),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.Roles)

            .WithErrorCode(ErrorCodes.UserRoleRequired);
    }

    [Fact]
    public async Task CreateUserValidator_WhenRolesAreDuplicatedIgnoringCase_ReturnsDuplicated()
    {
        var validator = new CreateUserCommandValidator(
            ValidUserManager());
        var result = await validator.TestValidateAsync(
            ValidCreate(roles: [ApplicationRoles.User, "user"]),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.Roles)

            .WithErrorCode(ErrorCodes.UserRoleDuplicated);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Unknown")]
    public async Task CreateUserValidator_WhenRoleIsStructurallyInvalid_ReturnsExpectedErrorWithoutIdentityLookup(
        string role)
    {
        var userManager = ValidUserManager();
        var validator = new CreateUserCommandValidator(
            userManager);
        var result = await validator.TestValidateAsync(
            ValidCreate(roles: [role]),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(
            result.Errors,
            x =>
                x.ErrorCode ==
                (string.IsNullOrEmpty(role)
                    ? ErrorCodes.UserRoleRequired
                    : ErrorCodes.UserRoleNotExists));
        await userManager.DidNotReceive().RoleExistsAsync(role, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUserValidator_WhenApplicationRoleDoesNotExistInIdentity_ReturnsNotExists()
    {
        var userManager = ValidUserManager();
        userManager
            .RoleExistsAsync(
                ApplicationRoles.User,
                Arg.Any<CancellationToken>())
            .Returns(false);
        var validator = new CreateUserCommandValidator(
            userManager);
        var result = await validator.TestValidateAsync(
            ValidCreate(),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result.Errors, x => x.ErrorCode == ErrorCodes.UserRoleNotExists);
    }

    [Fact]
    public async Task UpdateUserValidator_WhenCommandIsValid_HasNoErrors()
    {
        var userManager = ValidUserManager();
        userManager.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var validator = new UpdateUserCommandValidator(
            userManager);
        var result = await validator.TestValidateAsync(
            ValidUpdate(),
            cancellationToken: TestContext.Current.CancellationToken);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateUserValidator_WhenIdIsEmpty_ReturnsRequiredAndSkipsExistsLookup()
    {
        var userManager = ValidUserManager();
        var validator = new UpdateUserCommandValidator(
            userManager);
        var result = await validator.TestValidateAsync(
            ValidUpdate(id: Guid.Empty),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.Id)

            .WithErrorCode(ErrorCodes.UserIdRequired);
        await userManager.DidNotReceiveWithAnyArgs().ExistsAsync(default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UpdateUserValidator_WhenUserDoesNotExist_ReturnsNotExists()
    {
        var userManager = ValidUserManager();
        userManager.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var validator = new UpdateUserCommandValidator(
            userManager);
        var result = await validator.TestValidateAsync(
            ValidUpdate(),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.Id)

            .WithErrorCode(ErrorCodes.UserNotExists);
    }

    [Fact]
    public async Task UpdateUserValidator_WhenNamesPhoneAndRolesAreInvalid_ReturnsExpectedErrors()
    {
        var userManager = ValidUserManager();
        userManager.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var validator = new UpdateUserCommandValidator(
            userManager);
        var result = await validator.TestValidateAsync(
            ValidUpdate(lastName: "", firstName: "", phone: "123", roles: [ApplicationRoles.User, "user"]),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result.Errors, x => x.ErrorCode == ErrorCodes.UserLastNameRequired);
        Assert.Contains(result.Errors, x => x.ErrorCode == ErrorCodes.UserFirstNameRequired);
        Assert.Contains(result.Errors, x => x.ErrorCode == ErrorCodes.UserPhoneNumberInvalid);
        Assert.Contains(result.Errors, x => x.ErrorCode == ErrorCodes.UserRoleDuplicated);
    }

    [Fact]
    public async Task UpdateUserValidator_WhenRoleIsEmpty_ReturnsRequired()
    {
        var userManager = ValidUserManager();
        userManager.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var validator = new UpdateUserCommandValidator(
            userManager);
        var result = await validator.TestValidateAsync(
            ValidUpdate(roles: [""]),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result.Errors, x => x.ErrorCode == ErrorCodes.UserRoleRequired);
    }

    [Fact]
    public async Task UpdateUserValidator_WhenRoleIsUnknown_ReturnsNotExistsWithoutIdentityLookup()
    {
        var userManager = ValidUserManager();
        userManager.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var validator = new UpdateUserCommandValidator(
            userManager);
        var result = await validator.TestValidateAsync(
            ValidUpdate(roles: ["Unknown"]),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result.Errors, x => x.ErrorCode == ErrorCodes.UserRoleNotExists);
        await userManager.DidNotReceive().RoleExistsAsync("Unknown", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateUserValidator_WhenIdentityRoleIsMissing_ReturnsNotExists()
    {
        var userManager = ValidUserManager();
        userManager.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        userManager
            .RoleExistsAsync(
                ApplicationRoles.User,
                Arg.Any<CancellationToken>())
            .Returns(false);
        var validator = new UpdateUserCommandValidator(
            userManager);
        var result = await validator.TestValidateAsync(
            ValidUpdate(),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result.Errors, x => x.ErrorCode == ErrorCodes.UserRoleNotExists);
    }

    [Fact]
    public async Task ChangeEmailValidator_WhenEmailIsNull_ReturnsRequiredWithoutThrowing()
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var validator = CreateChangeEmailValidator(
            userManager,
            id,
            new TestCurrentUser(true, id));
        var command = new ChangeEmailUserCommand(id, "old@example.test", null!);

        var exception = await Record.ExceptionAsync(
            () => validator.TestValidateAsync(command, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(exception);
        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.NormalizedNewEmail)

            .WithErrorCode(ErrorCodes.UserNewEmailRequired);
    }

    [Fact]
    public async Task ChangeEmailValidator_WhenEmailIsTooLong_ReturnsExpectedError()
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var validator = CreateChangeEmailValidator(
            userManager,
            id,
            new TestCurrentUser(true, id));
        var email = new string('a', BudgetManager.Domain.Common.StringPropertyLengths.EmailLength + 1);
        var result = await validator.TestValidateAsync(
            new ChangeEmailUserCommand(id, "old@example.test", email),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.NormalizedNewEmail)

            .WithErrorCode(ErrorCodes.UserNewEmailTooLong);
    }

    [Fact]
    public async Task UpdateUserValidator_WhenRemovingAdministratorRoleFromLastActivatedAdministrator_ReturnsExpectedError()
    {
        var id = Guid.NewGuid();
        var userManager = ValidUserManager();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        userManager
            .IsLastActivatedAdministratorAsync(id, Arg.Any<CancellationToken>())
            .Returns(true);
        var validator = new UpdateUserCommandValidator(userManager);

        var result = await validator.TestValidateAsync(
            ValidUpdate(id: id, roles: [ApplicationRoles.User]),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(
            result.Errors,
            x => x.ErrorCode == ErrorCodes.UserIsLastActivatedAdministrator);
    }

    [Fact]
    public async Task UpdateUserValidator_WhenLastActivatedAdministratorKeepsAdministratorRole_HasNoErrorsAndSkipsLastAdministratorLookup()
    {
        var id = Guid.NewGuid();
        var userManager = ValidUserManager();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var validator = new UpdateUserCommandValidator(userManager);

        var result = await validator.TestValidateAsync(
            ValidUpdate(id: id, roles: [ApplicationRoles.Administrator]),
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldNotHaveAnyValidationErrors();
        await userManager.DidNotReceive()
            .IsLastActivatedAdministratorAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateUserValidator_WhenAnotherActivatedAdministratorExists_AllowsRemovingAdministratorRole()
    {
        var id = Guid.NewGuid();
        var userManager = ValidUserManager();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        userManager
            .IsLastActivatedAdministratorAsync(id, Arg.Any<CancellationToken>())
            .Returns(false);
        var validator = new UpdateUserCommandValidator(userManager);

        var result = await validator.TestValidateAsync(
            ValidUpdate(id: id, roles: [ApplicationRoles.User]),
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateUserValidator_WhenNamesAreTooLong_ReturnsExpectedErrors()
    {
        var userManager = ValidUserManager();
        userManager.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var validator = new UpdateUserCommandValidator(
            userManager);
        var result = await validator.TestValidateAsync(
            ValidUpdate(
                lastName: new string('x', BudgetManager.Domain.Common.StringPropertyLengths.LastNameLength + 1),
                firstName: new string('x', BudgetManager.Domain.Common.StringPropertyLengths.FirstNameLength + 1)),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result.Errors, x => x.ErrorCode == ErrorCodes.UserLastNameTooLong);
        Assert.Contains(result.Errors, x => x.ErrorCode == ErrorCodes.UserFirstNameTooLong);
    }

    [Fact]
    public async Task UpdateUserValidator_WhenPhoneIsTooLong_ReturnsExpectedError()
    {
        var userManager = ValidUserManager();
        userManager.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var validator = new UpdateUserCommandValidator(
            userManager);
        var result = await validator.TestValidateAsync(
            ValidUpdate(phone: "+1234567890123456"),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result.Errors, x => x.ErrorCode == ErrorCodes.UserPhoneNumberTooLong);
    }

    [Fact]
    public async Task ChangeEmailValidator_WhenIdIsEmpty_ReturnsRequiredAndStopsExistenceCheck()
    {
        var userManager = Substitute.For<IUserManager>();
        var validator = CreateChangeEmailValidator(
            userManager,
            Guid.Empty,
            new TestCurrentUser());
        var result = await validator.TestValidateAsync(
            new ChangeEmailUserCommand(Guid.Empty, "old@example.test", "new@example.test"),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.Id)

            .WithErrorCode(ErrorCodes.UserIdRequired);
        await userManager.DidNotReceiveWithAnyArgs().ExistsAsync(default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ChangeEmailValidator_WhenUserDoesNotExist_ReturnsNotExists()
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var validator = CreateChangeEmailValidator(
            userManager,
            id,
            new TestCurrentUser(true, id));
        var result = await validator.TestValidateAsync(
            new ChangeEmailUserCommand(id, "old@example.test", "new@example.test"),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.Id)

            .WithErrorCode(ErrorCodes.UserNotExists);
    }

    [Fact]
    public async Task ChangeEmailValidator_WhenValid_UsesNormalizedEmailAndExcludesCurrentId()
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        userManager
            .EmailIsCurrentAsync(
                id,
                "old@example.test",
                Arg.Any<CancellationToken>())
            .Returns(true);
        userManager
            .EmailExistsAsync(
                "new@example.test",
                id,
                Arg.Any<CancellationToken>())
            .Returns(false);
        var validator = CreateChangeEmailValidator(
            userManager,
            id,
            new TestCurrentUser(true, id));
        var result = await validator.TestValidateAsync(
            new ChangeEmailUserCommand(id, "old@example.test", "  new@example.test  "),
            cancellationToken: TestContext.Current.CancellationToken);
        result.ShouldNotHaveAnyValidationErrors();
        await userManager.Received(1).EmailExistsAsync("new@example.test", id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeEmailValidator_WhenCurrentEmailIsNotConfirmed_ReturnsExpectedError()
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        var validator = CreateChangeEmailValidator(
            userManager,
            id,
            new TestCurrentUser(true, id),
            emailConfirmed: false);

        var result = await validator.TestValidateAsync(
            new ChangeEmailUserCommand(id, "old@example.test", "new@example.test"),
            cancellationToken: TestContext.Current.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.UserEmailNotConfirmed);
    }

    [Theory]
    [InlineData("", ErrorCodes.UserNewEmailRequired)]
    [InlineData("not-email", ErrorCodes.UserNewEmailInvalid)]
    public async Task ChangeEmailValidator_WhenEmailInvalid_ReturnsExpectedError(string email, string errorCode)
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var validator = CreateChangeEmailValidator(
            userManager,
            id,
            new TestCurrentUser(true, id));
        var result = await validator.TestValidateAsync(
            new ChangeEmailUserCommand(id, "old@example.test", email),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(
            result.Errors,
            x =>
                x.PropertyName == nameof(ChangeEmailUserCommand.NormalizedNewEmail)
                && x.ErrorCode == errorCode);
    }

    [Fact]
    public async Task ChangeEmailValidator_WhenEmailAlreadyUsedByAnotherUser_ReturnsExpectedError()
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        userManager
            .EmailExistsAsync(
                "new@example.test",
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);
        var validator = CreateChangeEmailValidator(
            userManager,
            id,
            new TestCurrentUser(true, id));
        var result = await validator.TestValidateAsync(
            new ChangeEmailUserCommand(id, "old@example.test", "new@example.test"),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.NormalizedNewEmail)

            .WithErrorCode(ErrorCodes.UserNewEmailAlreadyUsed);
    }

    [Fact]
    public async Task ChangeEmailValidator_WhenUserIsNotCurrent_ReturnsExpectedError()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            CreateChangeEmailValidator(
                userManager,
                id,
                new TestCurrentUser(
                    true,
                    Guid.NewGuid()));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new ChangeEmailUserCommand(
                    id,
                    "old@example.test",
                    "new@example.test"),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Id)
            .WithErrorCode(
                ErrorCodes.UserNotCurrent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ChangeEmailValidator_WhenOldEmailIsEmpty_ReturnsRequiredAndStopsCurrentCheck(string? oldEmail)
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var validator = CreateChangeEmailValidator(
            userManager,
            id,
            new TestCurrentUser(true, id));

        var result = await validator.TestValidateAsync(
            new ChangeEmailUserCommand(id, oldEmail!, "new@example.test"),
            cancellationToken: TestContext.Current.CancellationToken);

        result

            .ShouldHaveValidationErrorFor(x => x.NormalizedOldEmail)

            .WithErrorCode(ErrorCodes.UserOldEmailRequired);
        await userManager
            .DidNotReceiveWithAnyArgs()
            .EmailIsCurrentAsync(
                default,
                default!,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ChangeEmailValidator_WhenOldEmailIsNotCurrent_ReturnsExpectedError()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        userManager
            .EmailIsCurrentAsync(
                id,
                "old@example.test",
                Arg.Any<CancellationToken>())
            .Returns(false);

        var validator =
            CreateChangeEmailValidator(
                userManager,
                id,
                new TestCurrentUser(
                    true,
                    id));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new ChangeEmailUserCommand(
                    id,
                    "old@example.test",
                    "new@example.test"),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.NormalizedOldEmail)
            .WithErrorCode(
                ErrorCodes.UserOldEmailNotCurrent);
    }

    [Fact]
    public async Task ChangeEmailValidator_WhenNewEmailEqualsOldEmail_ReturnsExpectedError()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        userManager
            .EmailIsCurrentAsync(
                id,
                "same@example.test",
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            CreateChangeEmailValidator(
                userManager,
                id,
                new TestCurrentUser(
                    true,
                    id));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new ChangeEmailUserCommand(
                    id,
                    " same@example.test ",
                    "same@example.test"),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.NormalizedNewEmail)
            .WithErrorCode(
                ErrorCodes.UserNewEmailEqualsToOldEmail);
    }

    [Fact]
    public async Task ConfirmEmailChangeValidator_WhenValid_UsesNormalizedEmailAndExcludesCurrentId()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        userManager
            .EmailExistsAsync(
                "new@example.test",
                id,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var validator =
            new ConfirmEmailChangeUserCommandValidator(
                userManager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new ConfirmEmailChangeUserCommand(
                    id,
                    "  new@example.test  ",
                    "token"),
                cancellationToken:
                    cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();

        await userManager
            .Received(1)
            .EmailExistsAsync(
                "new@example.test",
                id,
                cancellationToken);
    }

    [Fact]
    public async Task ConfirmEmailChangeValidator_WhenIdIsEmpty_ReturnsRequiredAndStopsExistenceCheck()
    {
        // Arrange

        var userManager =
            Substitute.For<IUserManager>();

        var validator =
            new ConfirmEmailChangeUserCommandValidator(
                userManager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new ConfirmEmailChangeUserCommand(
                    Guid.Empty,
                    "new@example.test",
                    "token"),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Id)
            .WithErrorCode(
                ErrorCodes.UserIdRequired);

        await userManager
            .DidNotReceiveWithAnyArgs()
            .ExistsAsync(
                default,
                cancellationToken);
    }

    [Fact]
    public async Task ConfirmEmailChangeValidator_WhenUserDoesNotExist_ReturnsNotExists()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var validator =
            new ConfirmEmailChangeUserCommandValidator(
                userManager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new ConfirmEmailChangeUserCommand(
                    id,
                    "new@example.test",
                    "token"),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Id)
            .WithErrorCode(
                ErrorCodes.UserNotExists);
    }

    [Fact]
    public async Task ConfirmEmailChangeValidator_WhenTokenIsEmpty_ReturnsRequired()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new ConfirmEmailChangeUserCommandValidator(
                userManager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new ConfirmEmailChangeUserCommand(
                    id,
                    "new@example.test",
                    string.Empty),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Token)
            .WithErrorCode(
                ErrorCodes.TokenRequired);
    }

    [Fact]
    public async Task ConfirmEmailChangeValidator_WhenEmailAlreadyUsed_ReturnsExpectedError()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        userManager
            .EmailExistsAsync(
                "used@example.test",
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new ConfirmEmailChangeUserCommandValidator(
                userManager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new ConfirmEmailChangeUserCommand(
                    id,
                    "used@example.test",
                    "token"),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.NormalizedEmail)
            .WithErrorCode(
                ErrorCodes.UserEmailAlreadyUsed);
    }

    [Fact]
    public async Task UpdateUserProfileValidator_WhenValid_HasNoErrors()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserProfileCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    id));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUserProfileCommand(
                    id,
                    "Last",
                    "First",
                    "06 12 34 56 78",
                    SupportedCultures.French,
                    SupportedThemes.System),
                cancellationToken:
                    cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateUserProfileValidator_WhenUserIsNotCurrent_ReturnsExpectedError()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserProfileCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    Guid.NewGuid()));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUserProfileCommand(
                    id,
                    "Last",
                    "First",
                    null,
                    SupportedCultures.French,
                    null),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Id)
            .WithErrorCode(
                ErrorCodes.UserNotCurrent);
    }

    [Fact]
    public async Task UpdateUserProfileValidator_WhenPreferencesAreUnsupported_ReturnsExpectedErrors()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserProfileCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    id));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUserProfileCommand(
                    id,
                    "Last",
                    "First",
                    null,
                    "xx-XX",
                    "unknown"),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.PreferredCulture)
            .WithErrorCode(
                ErrorCodes.CultureNotSupported);

        result
            .ShouldHaveValidationErrorFor(
                x => x.PreferredTheme)
            .WithErrorCode(
                ErrorCodes.ThemeNotSupported);
    }


    [Fact]
    public async Task UpdateUiPreferencesValidator_WhenValid_HasNoErrors()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUiPreferencesCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    id));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUiPreferencesCommand(
                    id,
                    SupportedCultures.English,
                    SupportedThemes.Dark),
                cancellationToken:
                    cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateUiPreferencesValidator_WhenUserIsNotCurrent_ReturnsExpectedError()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUiPreferencesCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    Guid.NewGuid()));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUiPreferencesCommand(
                    id,
                    SupportedCultures.French,
                    null),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Id)
            .WithErrorCode(
                ErrorCodes.UserNotCurrent);
    }

    [Fact]
    public async Task UpdateUiPreferencesValidator_WhenPreferencesAreUnsupported_ReturnsExpectedErrors()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUiPreferencesCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    id));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUiPreferencesCommand(
                    id,
                    "xx-XX",
                    "unknown"),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.PreferredCulture)
            .WithErrorCode(
                ErrorCodes.CultureNotSupported);

        result
            .ShouldHaveValidationErrorFor(
                x => x.PreferredTheme)
            .WithErrorCode(
                ErrorCodes.ThemeNotSupported);
    }

    [Fact]
    public async Task DeleteUserValidator_WhenValid_HasNoErrors()
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        userManager.IsBudgetOwnerAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        var validator = new DeleteUserCommandValidator(userManager, new TestCurrentUser(true, Guid.NewGuid()));
        var result = await validator.TestValidateAsync(
            new DeleteUserCommand(id),
            cancellationToken: TestContext.Current.CancellationToken);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DeleteUserValidator_WhenIdEmpty_ReturnsRequiredAndStopsFurtherChecks()
    {
        var userManager = Substitute.For<IUserManager>();
        var validator = new DeleteUserCommandValidator(userManager, new TestCurrentUser(true, Guid.NewGuid()));
        var result = await validator.TestValidateAsync(
            new DeleteUserCommand(Guid.Empty),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.Id)

            .WithErrorCode(ErrorCodes.UserIdRequired);
        await userManager.DidNotReceiveWithAnyArgs().ExistsAsync(default, TestContext.Current.CancellationToken);
        await userManager.DidNotReceiveWithAnyArgs().IsBudgetOwnerAsync(default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteUserValidator_WhenUserMissing_ReturnsNotExistsAndStopsOwnershipCheck()
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        var validator = new DeleteUserCommandValidator(userManager, new TestCurrentUser(true, Guid.NewGuid()));
        var result = await validator.TestValidateAsync(
            new DeleteUserCommand(id),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.Id)

            .WithErrorCode(ErrorCodes.UserNotExists);
        await userManager.DidNotReceiveWithAnyArgs().IsBudgetOwnerAsync(default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteUserValidator_WhenCurrentUser_ReturnsExpectedErrorAndStopsOwnershipCheck()
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var validator = new DeleteUserCommandValidator(userManager, new TestCurrentUser(true, id));
        var result = await validator.TestValidateAsync(
            new DeleteUserCommand(id),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.Id)

            .WithErrorCode(ErrorCodes.UserIsCurrentUser);
        await userManager.DidNotReceiveWithAnyArgs().IsBudgetOwnerAsync(default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteUserValidator_WhenBudgetOwner_ReturnsExpectedError()
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        userManager.IsBudgetOwnerAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var validator = new DeleteUserCommandValidator(userManager, new TestCurrentUser(true, Guid.NewGuid()));
        var result = await validator.TestValidateAsync(
            new DeleteUserCommand(id),
            cancellationToken: TestContext.Current.CancellationToken);
        result

            .ShouldHaveValidationErrorFor(x => x.Id)

            .WithErrorCode(ErrorCodes.UserIsBudgetOwner);
    }

    [Fact]
    public async Task DeleteUserValidator_WhenUserIsLastActivatedAdministrator_ReturnsExpectedError()
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        userManager.IsBudgetOwnerAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        userManager
            .IsLastActivatedAdministratorAsync(id, Arg.Any<CancellationToken>())
            .Returns(true);
        var validator = new DeleteUserCommandValidator(
            userManager,
            new TestCurrentUser(true, Guid.NewGuid()));

        var result = await validator.TestValidateAsync(
            new DeleteUserCommand(id),
            cancellationToken: TestContext.Current.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.UserIsLastActivatedAdministrator);
    }

    [Fact]
    public async Task DeleteUserValidator_WhenAnotherActivatedAdministratorExists_HasNoErrors()
    {
        var id = Guid.NewGuid();
        var userManager = Substitute.For<IUserManager>();
        userManager.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        userManager.IsBudgetOwnerAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        userManager
            .IsLastActivatedAdministratorAsync(id, Arg.Any<CancellationToken>())
            .Returns(false);
        var validator = new DeleteUserCommandValidator(
            userManager,
            new TestCurrentUser(true, Guid.NewGuid()));

        var result = await validator.TestValidateAsync(
            new DeleteUserCommand(id),
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static ChangeEmailUserCommandValidator CreateChangeEmailValidator(
        IUserManager userManager,
        Guid id,
        TestCurrentUser currentUser,
        bool emailConfirmed = true)
    {
        var userContext = Substitute.For<IUserContext>();
        userContext
            .GetRequiredAsync(id, Arg.Any<CancellationToken>())
            .Returns(new UserGetByIdDto(
                id,
                "user@example.test",
                "Last",
                "First",
                "user@example.test",
                emailConfirmed,
                SupportedCultures.French,
                null,
                null,
                null));

        return new ChangeEmailUserCommandValidator(
            userManager,
            userContext,
            currentUser);
    }

    private static CreateUserCommand ValidCreate(
        string email = "admin@example.test",
        string lastName = "Admin",
        string firstName = "User",
        string? phone = "+33612345678",
        IReadOnlyCollection<string>? roles = null,
        UserProfilePicture? profilePicture = null,
        string preferredCulture = SupportedCultures.French,
        string? preferredTheme = null,
        string activationPageName = "/Account/ActivateAccount",
        TimeSpan? activationTokenLifetime = null,
        string activationEmailSubject = "Activate your account")
        => new(
            email,
            lastName,
            firstName,
            phone,
            roles ?? [ApplicationRoles.User],
            profilePicture,
            preferredCulture,
            preferredTheme,
            activationPageName,
            new { area = "Identity" },
            activationTokenLifetime ?? TimeSpan.FromDays(7),
            activationEmailSubject);

    private static UpdateUserCommand ValidUpdate(
        Guid? id = null,
        string lastName = "Admin",
        string firstName = "User",
        string? phone = "+33612345678",
        IReadOnlyCollection<string>? roles = null,
        string preferredCulture = SupportedCultures.French,
        string? preferredTheme = null)
        => new(
            id ?? Guid.NewGuid(),
            lastName,
            firstName,
            phone,
            roles ?? [ApplicationRoles.User],
            preferredCulture,
            preferredTheme);

    private static IUserManager ValidUserManager()
    {
        var manager = Substitute.For<IUserManager>();
        manager.RoleExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        return manager;
    }

    [Fact]
    public async Task CreateUserValidator_WhenEmailContainsUnsupportedCharacters_ReturnsExpectedError()
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        var command =
            ValidCreate(
                email: "éric@example.test");

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.NormalizedEmail)
            .WithErrorCode(
                ErrorCodes.UserEmailInvalidCharacters);
    }

    [Theory]
    [InlineData("first.last@example.test")]
    [InlineData("first_last@example.test")]
    [InlineData("first-last@example.test")]
    [InlineData("first+tag@example.test")]
    public async Task CreateUserValidator_WhenEmailUsesSupportedSpecialCharacters_DoesNotReturnInvalidCharacters(
        string email)
    {
        // Arrange

        var userManager =
            ValidUserManager();

        userManager
            .EmailExistsAsync(
                email,
                null,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var validator =
            new CreateUserCommandValidator(
                userManager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(
                    email: email),
                cancellationToken:
                    cancellationToken);

        // Assert

        Assert.DoesNotContain(
            result.Errors,
            x => x.ErrorCode ==
                ErrorCodes.UserEmailInvalidCharacters);
    }

    [Fact]
    public async Task ChangeEmailValidator_WhenEmailContainsUnsupportedCharacters_ReturnsExpectedError()
    {
        // Arrange

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            CreateChangeEmailValidator(
                userManager,
                id,
                new TestCurrentUser(
                    true,
                    id));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new ChangeEmailUserCommand(
                    id,
                    "old@example.test",
                    "éric@example.test"),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.NormalizedNewEmail)
            .WithErrorCode(
                ErrorCodes.UserNewEmailInvalidCharacters);
    }

    [Fact]
    public async Task CreateUserValidator_WhenNamesAreAtMaximumLength_DoesNotReturnTooLongErrors()
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        var command =
            ValidCreate(
                lastName: new string(
                    'L',
                    BudgetManager.Domain.Common.StringPropertyLengths.LastNameLength),
                firstName: new string(
                    'F',
                    BudgetManager.Domain.Common.StringPropertyLengths.FirstNameLength));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        Assert.DoesNotContain(
            result.Errors,
            x => x.ErrorCode ==
                ErrorCodes.UserLastNameTooLong);

        Assert.DoesNotContain(
            result.Errors,
            x => x.ErrorCode ==
                ErrorCodes.UserFirstNameTooLong);
    }

    [Fact]
    public async Task UpdateUserValidator_WhenNamesAreAtMaximumLength_DoesNotReturnTooLongErrors()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        userManager
            .ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserCommandValidator(
                userManager);

        var command =
            ValidUpdate(
                lastName: new string(
                    'L',
                    BudgetManager.Domain.Common.StringPropertyLengths.LastNameLength),
                firstName: new string(
                    'F',
                    BudgetManager.Domain.Common.StringPropertyLengths.FirstNameLength));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        Assert.DoesNotContain(
            result.Errors,
            x => x.ErrorCode ==
                ErrorCodes.UserLastNameTooLong);

        Assert.DoesNotContain(
            result.Errors,
            x => x.ErrorCode ==
                ErrorCodes.UserFirstNameTooLong);
    }

    [Fact]
    public async Task CreateUserValidator_WhenPhoneIsAtMaximumE164Length_DoesNotReturnPhoneLengthOrFormatErrors()
    {
        // Arrange

        const string phone =
            "+123456789012345";

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(
                    phone: phone),
                cancellationToken:
                    cancellationToken);

        // Assert

        Assert.DoesNotContain(
            result.Errors,
            x => x.ErrorCode ==
                ErrorCodes.UserPhoneNumberTooLong);

        Assert.DoesNotContain(
            result.Errors,
            x => x.ErrorCode ==
                ErrorCodes.UserPhoneNumberInvalid);
    }

    [Fact]
    public async Task UpdateUserValidator_WhenPhoneIsAtMaximumE164Length_DoesNotReturnPhoneLengthOrFormatErrors()
    {
        // Arrange

        const string phone =
            "+123456789012345";

        var userManager =
            ValidUserManager();

        userManager
            .ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserCommandValidator(
                userManager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidUpdate(
                    phone: phone),
                cancellationToken:
                    cancellationToken);

        // Assert

        Assert.DoesNotContain(
            result.Errors,
            x => x.ErrorCode ==
                ErrorCodes.UserPhoneNumberTooLong);

        Assert.DoesNotContain(
            result.Errors,
            x => x.ErrorCode ==
                ErrorCodes.UserPhoneNumberInvalid);
    }

    [Fact]
    public async Task CreateUserValidator_WhenEmailIsAtMaximumLength_DoesNotReturnTooLongError()
    {
        // Arrange

        const string suffix =
            "@example.test";

        var email =
            new string(
                'a',
                BudgetManager.Domain.Common.StringPropertyLengths.EmailLength - suffix.Length)
            + suffix;

        var userManager =
            ValidUserManager();

        userManager
            .EmailExistsAsync(
                email,
                null,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var validator =
            new CreateUserCommandValidator(
                userManager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(
                    email: email),
                cancellationToken:
                    cancellationToken);

        // Assert

        Assert.DoesNotContain(
            result.Errors,
            x => x.ErrorCode ==
                ErrorCodes.UserEmailTooLong);
    }

    [Fact]
    public async Task ChangeEmailValidator_WhenEmailIsAtMaximumLength_DoesNotReturnTooLongError()
    {
        // Arrange

        const string suffix =
            "@example.test";

        var email =
            new string(
                'a',
                BudgetManager.Domain.Common.StringPropertyLengths.EmailLength - suffix.Length)
            + suffix;

        var id =
            Guid.NewGuid();

        var userManager =
            Substitute.For<IUserManager>();

        userManager
            .ExistsAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        userManager
            .EmailIsCurrentAsync(
                id,
                "old@example.test",
                Arg.Any<CancellationToken>())
            .Returns(true);

        userManager
            .EmailExistsAsync(
                email,
                id,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var validator =
            CreateChangeEmailValidator(
                userManager,
                id,
                new TestCurrentUser(
                    true,
                    id));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new ChangeEmailUserCommand(
                    id,
                    "old@example.test",
                    email),
                cancellationToken:
                    cancellationToken);

        // Assert

        Assert.DoesNotContain(
            result.Errors,
            x => x.ErrorCode ==
                ErrorCodes.UserNewEmailTooLong);
    }

    [Theory]
    [InlineData("")]
    [InlineData("de-DE")]
    public async Task CreateUserValidator_WhenPreferredCultureIsInvalid_ReturnsExpectedError(
        string preferredCulture)
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(
                    preferredCulture: preferredCulture),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.PreferredCulture)
            .WithErrorCode(
                ErrorCodes.CultureNotSupported);
    }

    [Theory]
    [InlineData(SupportedCultures.French)]
    [InlineData(SupportedCultures.English)]
    public async Task CreateUserValidator_WhenPreferredCultureIsSupported_HasNoCultureError(
        string preferredCulture)
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(
                    preferredCulture: preferredCulture),
                cancellationToken:
                    cancellationToken);

        // Assert

        result.ShouldNotHaveValidationErrorFor(
            x => x.PreferredCulture);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(SupportedThemes.Light)]
    [InlineData(SupportedThemes.Dark)]
    [InlineData(SupportedThemes.System)]
    public async Task CreateUserValidator_WhenPreferredThemeIsSupported_HasNoThemeError(
        string? preferredTheme)
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(
                    preferredTheme: preferredTheme),
                cancellationToken:
                    cancellationToken);

        // Assert

        result.ShouldNotHaveValidationErrorFor(
            x => x.PreferredTheme);
    }

    [Fact]
    public async Task CreateUserValidator_WhenPreferredThemeIsInvalid_ReturnsExpectedError()
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(
                    preferredTheme: "invalid"),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.PreferredTheme)
            .WithErrorCode(
                ErrorCodes.ThemeNotSupported);
    }

    [Fact]
    public async Task CreateUserValidator_WhenProfilePictureContentIsEmpty_ReturnsRequired()
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(
                    profilePicture:
                        new UserProfilePicture(
                            [],
                            SupportedPictureFormats.PngContentType)),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.ProfilePicture!.Content)
            .WithErrorCode(
                ErrorCodes.UserProfilePictureContentRequired);
    }

    [Fact]
    public async Task CreateUserValidator_WhenProfilePictureIsTooLarge_ReturnsExpectedError()
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(
                    profilePicture:
                        new UserProfilePicture(
                            new byte[500 * 1024 + 1],
                            SupportedPictureFormats.PngContentType)),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.ProfilePicture!.Content)
            .WithErrorCode(
                ErrorCodes.UserProfilePictureTooLarge);
    }

    [Fact]
    public async Task CreateUserValidator_WhenProfilePictureIsAtMaximumSize_HasNoSizeError()
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(
                    profilePicture:
                        new UserProfilePicture(
                            new byte[500 * 1024],
                            SupportedPictureFormats.PngContentType)),
                cancellationToken:
                    cancellationToken);

        // Assert

        Assert.DoesNotContain(
            result.Errors,
            x => x.ErrorCode ==
                ErrorCodes.UserProfilePictureTooLarge);
    }

    [Fact]
    public async Task CreateUserValidator_WhenProfilePictureContentTypeIsEmpty_ReturnsRequired()
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(
                    profilePicture:
                        new UserProfilePicture(
                            [1],
                            string.Empty)),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.ProfilePicture!.ContentType)
            .WithErrorCode(
                ErrorCodes.UserProfilePictureContentTypeRequired);
    }

    [Fact]
    public async Task CreateUserValidator_WhenProfilePictureContentTypeIsUnsupported_ReturnsExpectedError()
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(
                    profilePicture:
                        new UserProfilePicture(
                            [1],
                            "image/gif")),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.ProfilePicture!.ContentType)
            .WithErrorCode(
                ErrorCodes.UserProfilePictureFormatUnsupported);
    }

    [Fact]
    public async Task CreateUserValidator_WhenActivationPageNameIsEmpty_ReturnsRequired()
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(activationPageName: string.Empty),
                cancellationToken:
                    TestContext.Current.CancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.ActivationPageName)
            .WithErrorCode(
                ErrorCodes.UserActivationPageNameRequired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateUserValidator_WhenActivationTokenLifetimeIsNotPositive_ReturnsInvalid(
        int lifetimeTicks)
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(
                    activationTokenLifetime:
                        TimeSpan.FromTicks(lifetimeTicks)),
                cancellationToken:
                    TestContext.Current.CancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.ActivationTokenLifetime)
            .WithErrorCode(
                ErrorCodes.UserActivationTokenLifetimeInvalid);
    }

    [Fact]
    public async Task CreateUserValidator_WhenActivationEmailSubjectIsEmpty_ReturnsRequired()
    {
        // Arrange

        var validator =
            new CreateUserCommandValidator(
                ValidUserManager());

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidCreate(activationEmailSubject: string.Empty),
                cancellationToken:
                    TestContext.Current.CancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.ActivationEmailSubject)
            .WithErrorCode(
                ErrorCodes.UserActivationEmailSubjectRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData("de-DE")]
    public async Task UpdateUserValidator_WhenPreferredCultureIsInvalid_ReturnsExpectedError(
        string preferredCulture)
    {
        // Arrange

        var userManager =
            ValidUserManager();

        userManager
            .ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserCommandValidator(
                userManager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidUpdate(
                    preferredCulture: preferredCulture),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.PreferredCulture)
            .WithErrorCode(
                ErrorCodes.CultureNotSupported);
    }

    [Theory]
    [InlineData(SupportedCultures.French)]
    [InlineData(SupportedCultures.English)]
    public async Task UpdateUserValidator_WhenPreferredCultureIsSupported_HasNoCultureError(
        string preferredCulture)
    {
        // Arrange

        var userManager =
            ValidUserManager();

        userManager
            .ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserCommandValidator(
                userManager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidUpdate(
                    preferredCulture: preferredCulture),
                cancellationToken:
                    cancellationToken);

        // Assert

        result.ShouldNotHaveValidationErrorFor(
            x => x.PreferredCulture);
    }

    [Fact]
    public async Task UpdateUserValidator_WhenPreferredThemeIsInvalid_ReturnsExpectedError()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        userManager
            .ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserCommandValidator(
                userManager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidUpdate(
                    preferredTheme: "invalid"),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.PreferredTheme)
            .WithErrorCode(
                ErrorCodes.ThemeNotSupported);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(SupportedThemes.Light)]
    [InlineData(SupportedThemes.Dark)]
    [InlineData(SupportedThemes.System)]
    public async Task UpdateUserValidator_WhenPreferredThemeIsSupported_HasNoThemeError(
        string? preferredTheme)
    {
        // Arrange

        var userManager =
            ValidUserManager();

        userManager
            .ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserCommandValidator(
                userManager);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                ValidUpdate(
                    preferredTheme: preferredTheme),
                cancellationToken:
                    cancellationToken);

        // Assert

        result.ShouldNotHaveValidationErrorFor(
            x => x.PreferredTheme);
    }

    [Fact]
    public async Task UpdateUserProfilePictureValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var userId =
            Guid.NewGuid();

        userManager
            .ExistsAsync(
                userId,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    userId));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var command =
            new UpdateUserProfilePictureCommand(
                userId,
                new UserProfilePicture(
                    [1, 2, 3],
                    SupportedPictureFormats.PngContentType));

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateUserProfilePictureValidator_WhenUserIdIsEmpty_ReturnsRequiredAndSkipsExistsLookup()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var validator =
            new UpdateUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser());

        var cancellationToken =
            TestContext.Current.CancellationToken;

        var command =
            new UpdateUserProfilePictureCommand(
                Guid.Empty,
                new UserProfilePicture(
                    [1],
                    SupportedPictureFormats.PngContentType));

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.UserId)
            .WithErrorCode(
                ErrorCodes.UserIdRequired);

        await userManager
            .DidNotReceiveWithAnyArgs()
            .ExistsAsync(
                default,
                cancellationToken);
    }

    [Fact]
    public async Task UpdateUserProfilePictureValidator_WhenPictureIsNull_ReturnsRequired()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var userId =
            Guid.NewGuid();

        userManager
            .ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    userId));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUserProfilePictureCommand(
                    userId,
                    null!),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.ProfilePicture)
            .WithErrorCode(
                ErrorCodes.UserProfilePictureRequired);
    }

    [Fact]
    public async Task UpdateUserProfilePictureValidator_WhenUserDoesNotExist_ReturnsExpectedError()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var userId =
            Guid.NewGuid();

        userManager
            .ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var validator =
            new UpdateUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    userId));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUserProfilePictureCommand(
                    userId,
                    new UserProfilePicture(
                        [1],
                        SupportedPictureFormats.PngContentType)),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.UserId)
            .WithErrorCode(
                ErrorCodes.UserNotExists);
    }

    [Fact]
    public async Task UpdateUserProfilePictureValidator_WhenContentIsEmpty_ReturnsRequired()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var userId =
            Guid.NewGuid();

        userManager
            .ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    userId));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUserProfilePictureCommand(
                    userId,
                    new UserProfilePicture(
                        [],
                        SupportedPictureFormats.PngContentType)),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.ProfilePicture.Content)
            .WithErrorCode(
                ErrorCodes.UserProfilePictureContentRequired);
    }

    [Fact]
    public async Task UpdateUserProfilePictureValidator_WhenPictureIsTooLarge_ReturnsExpectedError()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var userId =
            Guid.NewGuid();

        userManager
            .ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    userId));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUserProfilePictureCommand(
                    userId,
                    new UserProfilePicture(
                        new byte[500 * 1024 + 1],
                        SupportedPictureFormats.PngContentType)),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.ProfilePicture.Content)
            .WithErrorCode(
                ErrorCodes.UserProfilePictureTooLarge);
    }

    [Fact]
    public async Task UpdateUserProfilePictureValidator_WhenPictureIsAtMaximumSize_HasNoSizeError()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var userId =
            Guid.NewGuid();

        userManager
            .ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    userId));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUserProfilePictureCommand(
                    userId,
                    new UserProfilePicture(
                        new byte[500 * 1024],
                        SupportedPictureFormats.PngContentType)),
                cancellationToken:
                    cancellationToken);

        // Assert

        Assert.DoesNotContain(
            result.Errors,
            x => x.ErrorCode ==
                ErrorCodes.UserProfilePictureTooLarge);
    }

    [Fact]
    public async Task UpdateUserProfilePictureValidator_WhenContentTypeIsEmpty_ReturnsRequired()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var userId =
            Guid.NewGuid();

        userManager
            .ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    userId));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUserProfilePictureCommand(
                    userId,
                    new UserProfilePicture(
                        [1],
                        string.Empty)),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.ProfilePicture.ContentType)
            .WithErrorCode(
                ErrorCodes.UserProfilePictureContentTypeRequired);
    }

    [Fact]
    public async Task UpdateUserProfilePictureValidator_WhenContentTypeIsUnsupported_ReturnsExpectedError()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var userId =
            Guid.NewGuid();

        userManager
            .ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    userId));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUserProfilePictureCommand(
                    userId,
                    new UserProfilePicture(
                        [1],
                        "image/gif")),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.ProfilePicture.ContentType)
            .WithErrorCode(
                ErrorCodes.UserProfilePictureFormatUnsupported);
    }

    [Fact]
    public async Task UpdateUserProfilePictureValidator_WhenUserIsNotCurrent_ReturnsExpectedError()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var userId =
            Guid.NewGuid();

        userManager
            .ExistsAsync(
                userId,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new UpdateUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    Guid.NewGuid()));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new UpdateUserProfilePictureCommand(
                    userId,
                    new UserProfilePicture(
                        [1],
                        SupportedPictureFormats.PngContentType)),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.UserId)
            .WithErrorCode(
                ErrorCodes.UserNotCurrent);
    }

    [Fact]
    public async Task DeleteUserProfilePictureValidator_WhenPictureExists_HasNoErrors()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var userId =
            Guid.NewGuid();

        userManager
            .ExistsAsync(
                userId,
                Arg.Any<CancellationToken>())
            .Returns(true);

        userManager
            .ProfilePictureExistsAsync(
                userId,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new DeleteUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    userId));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new DeleteUserProfilePictureCommand(
                    userId),
                cancellationToken:
                    cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DeleteUserProfilePictureValidator_WhenUserIdIsEmpty_ReturnsRequiredAndSkipsLookups()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var validator =
            new DeleteUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser());

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new DeleteUserProfilePictureCommand(
                    Guid.Empty),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.UserId)
            .WithErrorCode(
                ErrorCodes.UserIdRequired);

        await userManager
            .DidNotReceiveWithAnyArgs()
            .ExistsAsync(
                default,
                cancellationToken);

        await userManager
            .DidNotReceiveWithAnyArgs()
            .ProfilePictureExistsAsync(
                default,
                cancellationToken);
    }

    [Fact]
    public async Task DeleteUserProfilePictureValidator_WhenUserDoesNotExist_ReturnsNotExistsAndSkipsPictureLookup()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var userId =
            Guid.NewGuid();

        userManager
            .ExistsAsync(
                userId,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var validator =
            new DeleteUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    userId));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new DeleteUserProfilePictureCommand(
                    userId),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.UserId)
            .WithErrorCode(
                ErrorCodes.UserNotExists);

        await userManager
            .DidNotReceiveWithAnyArgs()
            .ProfilePictureExistsAsync(
                default,
                cancellationToken);
    }

    [Fact]
    public async Task DeleteUserProfilePictureValidator_WhenUserIsNotCurrent_ReturnsExpectedErrorAndSkipsPictureLookup()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var userId =
            Guid.NewGuid();

        userManager
            .ExistsAsync(
                userId,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var validator =
            new DeleteUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    Guid.NewGuid()));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new DeleteUserProfilePictureCommand(
                    userId),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.UserId)
            .WithErrorCode(
                ErrorCodes.UserNotCurrent);

        await userManager
            .DidNotReceiveWithAnyArgs()
            .ProfilePictureExistsAsync(
                default,
                cancellationToken);
    }

    [Fact]
    public async Task DeleteUserProfilePictureValidator_WhenPictureDoesNotExist_ReturnsExpectedError()
    {
        // Arrange

        var userManager =
            ValidUserManager();

        var userId =
            Guid.NewGuid();

        userManager
            .ExistsAsync(
                userId,
                Arg.Any<CancellationToken>())
            .Returns(true);

        userManager
            .ProfilePictureExistsAsync(
                userId,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var validator =
            new DeleteUserProfilePictureCommandValidator(
                userManager,
                new TestCurrentUser(
                    true,
                    userId));

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                new DeleteUserProfilePictureCommand(
                    userId),
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.UserId)
            .WithErrorCode(
                ErrorCodes.UserProfilePictureNotExists);
    }

}

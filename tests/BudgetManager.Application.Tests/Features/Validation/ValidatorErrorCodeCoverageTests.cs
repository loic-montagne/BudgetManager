using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Features.Account.Create;
using BudgetManager.Application.Features.Bank.Create;
using BudgetManager.Application.Features.Budget.Create;
using BudgetManager.Application.Features.Budget.DissociateCategory;
using BudgetManager.Application.Features.Budget.TransferOwnership;
using BudgetManager.Application.Features.Budget.UpdateAccess;
using BudgetManager.Application.Features.BudgetAccess.GetByKey;
using BudgetManager.Application.Features.BudgetCategory.Create;
using BudgetManager.Application.Features.Transaction.Create;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.ValueObjects;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class ValidatorErrorCodeCoverageTests
{
    [Fact]
    public async Task CreateAccountValidator_WhenRequiredValuesAreMissing_ReturnsExpectedErrors()
    {
        var validator = new CreateAccountCommandValidator(
            Substitute.For<IAccountRepository>(),
            Substitute.For<IBankContext>());

        var result = await validator.TestValidateAsync(
            new CreateAccountCommand(
                string.Empty,
                string.Empty,
                Guid.Empty),
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(ErrorCodes.AccountNameRequired);

        result.ShouldHaveValidationErrorFor(x => x.Iban)
            .WithErrorCode(ErrorCodes.AccountIbanRequired);

        result.ShouldHaveValidationErrorFor(x => x.BankId)
            .WithErrorCode(ErrorCodes.AccountBankRequired);
    }

    [Fact]
    public async Task CreateAccountValidator_WhenBankDoesNotExist_ReturnsExpectedError()
    {
        var repository = Substitute.For<IAccountRepository>();
        var bankContext = Substitute.For<IBankContext>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var bankId = Guid.NewGuid();

        repository
            .IsNameUniqueAsync(
                Arg.Any<string>(),
                null,
                cancellationToken)
            .Returns(true);

        repository
            .IsIbanUniqueAsync(
                Arg.Any<Iban>(),
                null,
                cancellationToken)
            .Returns(true);

        bankContext
            .ExistsAsync(
                bankId,
                cancellationToken)
            .Returns(false);

        var validator = new CreateAccountCommandValidator(
            repository,
            bankContext);

        var result = await validator.TestValidateAsync(
            new CreateAccountCommand(
                "Account",
                "FR7630006000011234567890189",
                bankId),
            cancellationToken: cancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.BankId)
            .WithErrorCode(ErrorCodes.AccountBankNotExists);
    }

    [Fact]
    public async Task CreateAccountValidator_WhenIbanAlreadyExists_ReturnsExpectedError()
    {
        var repository = Substitute.For<IAccountRepository>();
        var bankContext = Substitute.For<IBankContext>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var bankId = Guid.NewGuid();

        repository
            .IsNameUniqueAsync(
                Arg.Any<string>(),
                null,
                cancellationToken)
            .Returns(true);

        repository
            .IsIbanUniqueAsync(
                Arg.Any<Iban>(),
                null,
                cancellationToken)
            .Returns(false);

        bankContext
            .ExistsAsync(
                bankId,
                cancellationToken)
            .Returns(true);

        var validator = new CreateAccountCommandValidator(
            repository,
            bankContext);

        var result = await validator.TestValidateAsync(
            new CreateAccountCommand(
                "Account",
                "FR7630006000011234567890189",
                bankId),
            cancellationToken: cancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Iban)
            .WithErrorCode(ErrorCodes.AccountIbanAlreadyUsed);
    }

    [Fact]
    public async Task CreateBankValidator_WhenRequiredValuesAreMissing_ReturnsExpectedErrors()
    {
        var validator = new CreateBankCommandValidator(
            Substitute.For<IBankRepository>());

        var result = await validator.TestValidateAsync(
            new CreateBankCommand(
                string.Empty,
                string.Empty),
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(ErrorCodes.BankNameRequired);

        result.ShouldHaveValidationErrorFor(x => x.Bic)
            .WithErrorCode(ErrorCodes.BankBicRequired);
    }

    [Fact]
    public async Task CreateBankValidator_WhenBicAlreadyExists_ReturnsExpectedError()
    {
        var repository = Substitute.For<IBankRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository
            .IsNameUniqueAsync(
                Arg.Any<string>(),
                null,
                cancellationToken)
            .Returns(true);

        repository
            .IsBicUniqueAsync(
                Arg.Any<Bic>(),
                null,
                cancellationToken)
            .Returns(false);

        var validator = new CreateBankCommandValidator(
            repository);

        var result = await validator.TestValidateAsync(
            new CreateBankCommand(
                "Bank",
                "BNPAFRPP"),
            cancellationToken: cancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Bic)
            .WithErrorCode(ErrorCodes.BankBicAlreadyUsed);
    }

    [Fact]
    public async Task CreateBudgetCategoryValidator_WhenNameIsMissing_ReturnsExpectedError()
    {
        var validator = new CreateBudgetCategoryCommandValidator(
            Substitute.For<IBudgetCategoryRepository>());

        var result = await validator.TestValidateAsync(
            new CreateBudgetCategoryCommand(
                string.Empty,
                null),
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(ErrorCodes.BudgetCategoryNameRequired);
    }

    [Fact]
    public async Task CreateBudgetValidator_WhenNameIsMissing_ReturnsExpectedError()
    {
        var validator = new CreateBudgetCommandValidator(
            Substitute.For<IBudgetRepository>());

        var result = await validator.TestValidateAsync(
            new CreateBudgetCommand(
                string.Empty),
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(ErrorCodes.BudgetNameRequired);
    }

    [Fact]
    public async Task GetBudgetAccessByKeyValidator_WhenIdentifiersAreMissing_ReturnsExpectedErrors()
    {
        var validator =
            new GetBudgetAccessByKeyQueryValidator();

        var result = await validator.TestValidateAsync(
            new GetBudgetAccessByKeyQuery(
                Guid.Empty,
                Guid.Empty),
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.BudgetId)
            .WithErrorCode(ErrorCodes.BudgetAccessBudgetIdRequired);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode(ErrorCodes.BudgetAccessUserIdRequired);
    }

    [Fact]
    public async Task TransferBudgetOwnershipValidator_WhenTargetIsCurrentUser_ReturnsExpectedError()
    {
        var currentUserId = Guid.NewGuid();
        var currentUser =
            new TestCurrentUser(
                userId: currentUserId);

        var validator =
            new TransferBudgetOwnershipCommandValidator(
                Substitute.For<IBudgetContext>(),
                Substitute.For<IUserContext>(),
                currentUser);

        var result = await validator.TestValidateAsync(
            new TransferBudgetOwnershipCommand(
                Guid.NewGuid(),
                currentUserId),
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode(ErrorCodes.BudgetUserIsCurrent);
    }

    [Fact]
    public async Task TransferBudgetOwnershipValidator_WhenTargetDoesNotExist_ReturnsExpectedError()
    {
        var currentUser =
            new TestCurrentUser();

        var userContext =
            Substitute.For<IUserContext>();

        var targetUserId =
            Guid.NewGuid();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        userContext
            .ExistsAsync(
                targetUserId,
                cancellationToken)
            .Returns(false);

        var validator =
            new TransferBudgetOwnershipCommandValidator(
                Substitute.For<IBudgetContext>(),
                userContext,
                currentUser);

        var result = await validator.TestValidateAsync(
            new TransferBudgetOwnershipCommand(
                Guid.NewGuid(),
                targetUserId),
            cancellationToken: cancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode(ErrorCodes.BudgetUserNotExists);
    }

    [Fact]
    public async Task TransferBudgetOwnershipValidator_WhenCurrentUserIsNotOwner_ReturnsExpectedError()
    {
        var budgetContext =
            Substitute.For<IBudgetContext>();

        var userContext =
            Substitute.For<IUserContext>();

        var currentUser =
            new TestCurrentUser();

        var budgetId =
            Guid.NewGuid();

        var targetUserId =
            Guid.NewGuid();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        userContext
            .ExistsAsync(
                targetUserId,
                cancellationToken)
            .Returns(true);

        budgetContext
            .ExistsAsync(
                budgetId,
                cancellationToken)
            .Returns(true);

        budgetContext
            .IsOwnerAsync(
                budgetId,
                currentUser.UserId!.Value,
                cancellationToken)
            .Returns(false);

        budgetContext
            .IsNotOwnerAsync(
                budgetId,
                targetUserId,
                cancellationToken)
            .Returns(true);

        var validator =
            new TransferBudgetOwnershipCommandValidator(
                budgetContext,
                userContext,
                currentUser);

        var result = await validator.TestValidateAsync(
            new TransferBudgetOwnershipCommand(
                budgetId,
                targetUserId),
            cancellationToken: cancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.BudgetId)
            .WithErrorCode(ErrorCodes.BudgetUserIsNotOwner);
    }

    [Fact]
    public async Task UpdateBudgetAccessValidator_WhenTargetUserDoesNotExist_ReturnsExpectedError()
    {
        var userContext =
            Substitute.For<IUserContext>();

        var userId =
            Guid.NewGuid();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        userContext
            .ExistsAsync(
                userId,
                cancellationToken)
            .Returns(false);

        var validator =
            new UpdateBudgetAccessCommandValidator(
                Substitute.For<IBudgetContext>(),
                userContext,
                new TestCurrentUser());

        var result = await validator.TestValidateAsync(
            new UpdateBudgetAccessCommand(
                Guid.NewGuid(),
                userId,
                Permission.View),
            cancellationToken: cancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode(ErrorCodes.BudgetUserNotExists);
    }

    [Fact]
    public async Task DissociateCategoryValidator_WhenCategoryIdIsMissing_ReturnsExpectedError()
    {
        var validator =
            new DissociateCategoryCommandValidator(
                Substitute.For<IBudgetContext>(),
                Substitute.For<IBudgetCategoryContext>(),
                Substitute.For<IBudgetCategoryRepository>());

        var result = await validator.TestValidateAsync(
            new DissociateCategoryCommand(
                Guid.NewGuid(),
                Guid.Empty),
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorCode(ErrorCodes.BudgetBudgetCategoryRequired);
    }

    [Fact]
    public async Task DissociateCategoryValidator_WhenCategoryIsNotAssociated_ReturnsExpectedError()
    {
        var fixture =
            CreateDissociateFixture();

        fixture.CategoryContext
            .IsAssociatedToBudgetAsync(
                fixture.Category.Id,
                fixture.Budget.Id,
                fixture.CancellationToken)
            .Returns(false);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorCode(ErrorCodes.BudgetBudgetCategoryNotAssociated);
    }

    [Fact]
    public async Task DissociateCategoryValidator_WhenCategoryIsUsedInBudget_ReturnsExpectedError()
    {
        var fixture =
            CreateDissociateFixture();

        fixture.CategoryRepository
            .IsUsedAsync(
                fixture.Category.Id,
                fixture.Budget.Id,
                fixture.CancellationToken)
            .Returns(true);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorCode(ErrorCodes.BudgetBudgetCategoryIsUsedInBudget);
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenRequiredValuesAreMissing_ReturnsExpectedErrors()
    {
        var budgetContext =
            Substitute.For<IBudgetContext>();

        var budgetId =
            Guid.NewGuid();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        budgetContext
            .ExistsAsync(
                budgetId,
                cancellationToken)
            .Returns(true);

        budgetContext
            .IsEditableAsync(
                budgetId,
                cancellationToken)
            .Returns(BudgetEditableStatus.Editable);

        var validator =
            new CreateTransactionCommandValidator(
                budgetContext,
                Substitute.For<IBudgetCategoryContext>(),
                Substitute.For<IAccountContext>(),
                Substitute.For<ITransactionRepository>());

        var result = await validator.TestValidateAsync(
            new CreateTransactionCommand(
                budgetId,
                Guid.Empty,
                Guid.Empty,
                string.Empty,
                TransactionType.Expense,
                10m,
                PaymentMethod.Cash,
                null),
            cancellationToken: cancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorCode(ErrorCodes.TransactionCategoryRequired);

        result.ShouldHaveValidationErrorFor(x => x.AccountId)
            .WithErrorCode(ErrorCodes.TransactionAccountRequired);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(ErrorCodes.TransactionNameRequired);
    }

    private static DissociateFixture CreateDissociateFixture()
    {
        var ownerId =
            Guid.NewGuid();

        var budget =
            Budget.Create(
                "Budget",
                ownerId);

        var category =
            BudgetCategory.Create(
                "Category",
                null);

        budget.AssociateCategory(
            category,
            ownerId);

        var budgetContext =
            Substitute.For<IBudgetContext>();

        var categoryContext =
            Substitute.For<IBudgetCategoryContext>();

        var categoryRepository =
            Substitute.For<IBudgetCategoryRepository>();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        budgetContext
            .ExistsAsync(
                budget.Id,
                cancellationToken)
            .Returns(true);

        budgetContext
            .IsEditableAsync(
                budget.Id,
                cancellationToken)
            .Returns(BudgetEditableStatus.Editable);

        budgetContext
            .GetAsync(
                budget.Id,
                cancellationToken)
            .Returns(budget);

        categoryContext
            .ExistsAsync(
                category.Id,
                cancellationToken)
            .Returns(true);

        categoryContext
            .GetAsync(
                category.Id,
                cancellationToken)
            .Returns(category);

        categoryContext
            .IsAssociatedToBudgetAsync(
                category.Id,
                budget.Id,
                cancellationToken)
            .Returns(true);

        categoryRepository
            .IsUsedAsync(
                category.Id,
                budget.Id,
                cancellationToken)
            .Returns(false);

        var validator =
            new DissociateCategoryCommandValidator(
                budgetContext,
                categoryContext,
                categoryRepository);

        return new DissociateFixture(
            validator,
            budgetContext,
            categoryContext,
            categoryRepository,
            budget,
            category,
            new DissociateCategoryCommand(
                budget.Id,
                category.Id),
            cancellationToken);
    }

    private sealed record DissociateFixture(
        DissociateCategoryCommandValidator Validator,
        IBudgetContext BudgetContext,
        IBudgetCategoryContext CategoryContext,
        IBudgetCategoryRepository CategoryRepository,
        Budget Budget,
        BudgetCategory Category,
        DissociateCategoryCommand Command,
        CancellationToken CancellationToken);
}

using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Features.Account.Create;
using BudgetManager.Application.Features.Account.Search;
using BudgetManager.Application.Features.Bank.Create;
using BudgetManager.Application.Features.Bank.Search;
using BudgetManager.Application.Features.Budget.Search;
using BudgetManager.Application.Features.BudgetCategory.Search;
using BudgetManager.Application.Features.User.Search;
using BudgetManager.Application.Features.Budget.AssociateCategory;
using BudgetManager.Application.Features.Budget.ReorderCategories;
using BudgetManager.Application.Features.Budget.TransferOwnership;
using BudgetManager.Application.Features.Budget.UpdateAccess;
using BudgetManager.Application.Features.Transaction.Update;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class ComprehensiveRegressionValidatorTests
{
    [Fact]
    public async Task TransferBudgetOwnershipValidator_WhenBudgetExistsAndUserIdIsEmpty_ReturnsRequiredWithoutCallingOwnerCheck()
    {
        var budgetContext = Substitute.For<IBudgetContext>();
        var userContext = Substitute.For<IUserContext>();
        var currentUser = new TestCurrentUser();
        var budgetId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsOwnerAsync(budgetId, currentUser.RequiredUserId, cancellationToken).Returns(true);

        var validator = new TransferBudgetOwnershipCommandValidator(
            budgetContext,
            userContext,
            currentUser);

        var result = await validator.TestValidateAsync(
            new TransferBudgetOwnershipCommand(budgetId, Guid.Empty),
            cancellationToken: cancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode(ErrorCodes.BudgetUserRequired);

        await budgetContext.DidNotReceive().IsNotOwnerAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBudgetAccessValidator_WhenBudgetExistsAndUserIdIsEmpty_ReturnsRequiredWithoutCallingOwnerCheck()
    {
        var budgetContext = Substitute.For<IBudgetContext>();
        var userContext = Substitute.For<IUserContext>();
        var currentUser = new TestCurrentUser();
        var budgetId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.HasCurrentUserPermissionAsync(budgetId, Permission.Share, cancellationToken).Returns(true);

        var validator = new UpdateBudgetAccessCommandValidator(
            budgetContext,
            userContext,
            currentUser);

        var result = await validator.TestValidateAsync(
            new UpdateBudgetAccessCommand(budgetId, Guid.Empty, Permission.View),
            cancellationToken: cancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode(ErrorCodes.BudgetUserRequired);

        await budgetContext.DidNotReceive().IsNotOwnerAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task AssociateCategoryValidator_WhenBudgetExistsAndCategoryIdIsEmpty_DoesNotRunAssociationCheck()
    {
        var budgetContext = Substitute.For<IBudgetContext>();
        var categoryContext = Substitute.For<IBudgetCategoryContext>();
        var budgetId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsEditableAsync(budgetId, cancellationToken).Returns(BudgetEditableStatus.Editable);

        var validator = new AssociateCategoryCommandValidator(
            budgetContext,
            categoryContext);

        var result = await validator.TestValidateAsync(
            new AssociateCategoryCommand(budgetId, Guid.Empty),
            cancellationToken: cancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorCode(ErrorCodes.BudgetBudgetCategoryRequired);

        await categoryContext.DidNotReceive().IsAssociatedToBudgetAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAccountsValidator_WhenPaginationIsPartiallySpecified_ReturnsPaginationError()
    {
        var validator = new SearchAccountsQueryValidator();
        var query = new SearchAccountsQuery(
            new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
                null,
                true,
                [],
                null,
                0,
                null,
                null));

        var result = await validator.TestValidateAsync(query, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(
            result.Errors,
            x => x.ErrorCode == ErrorCodes.SearchPaginationInvalid);
    }

    [Fact]
    public async Task SearchAccountsValidator_WhenSortDirectionIsInvalid_ReturnsExpectedError()
    {
        var validator = new SearchAccountsQueryValidator();
        var query = new SearchAccountsQuery(
            new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
                null,
                true,
                [],
                null,
                null,
                null,
                [new SortCriterion<AccountSortField>(AccountSortField.Name, (SortDirection)999)]));

        var result = await validator.TestValidateAsync(query, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(
            result.Errors,
            x => x.ErrorCode == ErrorCodes.SearchSortDirectionInvalid);
    }

    [Fact]
    public async Task SearchAccountsValidator_WhenSortFieldIsInvalid_ReturnsExpectedError()
    {
        var validator = new SearchAccountsQueryValidator();
        var query = new SearchAccountsQuery(
            new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
                null,
                true,
                [],
                null,
                null,
                null,
                [new SortCriterion<AccountSortField>((AccountSortField)999, SortDirection.Ascending)]));

        var result = await validator.TestValidateAsync(query, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(
            result.Errors,
            x => x.ErrorCode == ErrorCodes.SearchSortFieldInvalid);
    }


    [Fact]
    public async Task EverySearchValidator_WhenSortIsInvalid_ReturnsSortErrors()
    {
        var bankResult =
            await new SearchBanksQueryValidator()
                .TestValidateAsync(
                    new SearchBanksQuery(
                        new PagedSearchCriteria<BankSortField>(
                            null,
                            null,
                            null,
                            [
                                new SortCriterion<BankSortField>(
                                    (BankSortField)999,
                                    (SortDirection)999)
                            ])),
                    cancellationToken:
                        TestContext.Current.CancellationToken);

        var budgetResult =
            await new SearchBudgetsQueryValidator()
                .TestValidateAsync(
                    new SearchBudgetsQuery(
                        new BudgetManager.Application.Features.Budget.Search.PagedSearchCriteria(
                            null,
                            null,
                            null,
                            null,
                            [
                                new SortCriterion<BudgetSortField>(
                                    (BudgetSortField)999,
                                    (SortDirection)999)
                            ])),
                    cancellationToken:
                        TestContext.Current.CancellationToken);

        var categoryResult =
            await new SearchBudgetCategoriesQueryValidator()
                .TestValidateAsync(
                    new SearchBudgetCategoriesQuery(
                        new PagedSearchCriteria<BudgetCategorySortField>(
                            null,
                            null,
                            null,
                            [
                                new SortCriterion<BudgetCategorySortField>(
                                    (BudgetCategorySortField)999,
                                    (SortDirection)999)
                            ])),
                    cancellationToken:
                        TestContext.Current.CancellationToken);

        var userResult =
            await new SearchUsersQueryValidator()
                .TestValidateAsync(
                    new SearchUsersQuery(
                        new Features.User.Search.PagedSearchCriteria(
                            null,
                            null,
                            null,
                            null,
                            [
                                new SortCriterion<UserSortField>(
                                    (UserSortField)999,
                                    (SortDirection)999)
                            ])),
                    cancellationToken:
                        TestContext.Current.CancellationToken);

        Assert.Contains(bankResult.Errors, x => x.ErrorCode == ErrorCodes.SearchSortFieldInvalid);
        Assert.Contains(bankResult.Errors, x => x.ErrorCode == ErrorCodes.SearchSortDirectionInvalid);
        Assert.Contains(budgetResult.Errors, x => x.ErrorCode == ErrorCodes.SearchSortFieldInvalid);
        Assert.Contains(budgetResult.Errors, x => x.ErrorCode == ErrorCodes.SearchSortDirectionInvalid);
        Assert.Contains(categoryResult.Errors, x => x.ErrorCode == ErrorCodes.SearchSortFieldInvalid);
        Assert.Contains(categoryResult.Errors, x => x.ErrorCode == ErrorCodes.SearchSortDirectionInvalid);
        Assert.Contains(userResult.Errors, x => x.ErrorCode == ErrorCodes.SearchSortFieldInvalid);
        Assert.Contains(userResult.Errors, x => x.ErrorCode == ErrorCodes.SearchSortDirectionInvalid);
    }

    [Fact]
    public async Task UpdateTransactionValidator_WhenNameIsMissing_ReturnsRequiredError()
    {
        var fixture = CreateUpdateTransactionFixture();
        var command = fixture.Command with { Name = string.Empty };

        var result = await fixture.Validator.TestValidateAsync(
            command,
            cancellationToken: fixture.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(ErrorCodes.TransactionNameRequired);
    }

    [Fact]
    public async Task UpdateTransactionValidator_WhenAmountIsZero_ReturnsInvalidAmountError()
    {
        var fixture = CreateUpdateTransactionFixture();
        var command = fixture.Command with { Amount = 0m };

        var result = await fixture.Validator.TestValidateAsync(
            command,
            cancellationToken: fixture.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorCode(ErrorCodes.TransactionAmountInvalid);
    }

    [Fact]
    public async Task UpdateTransactionValidator_WhenMethodIsInvalid_ReturnsInvalidMethodError()
    {
        var fixture = CreateUpdateTransactionFixture();
        var command = fixture.Command with { Method = (PaymentMethod)999 };

        var result = await fixture.Validator.TestValidateAsync(
            command,
            cancellationToken: fixture.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Method)
            .WithErrorCode(ErrorCodes.TransactionMethodInvalid);
    }

    [Fact]
    public async Task UpdateTransactionValidator_WhenBankTransferHasNoTransferAccount_ReturnsRequiredError()
    {
        var fixture = CreateUpdateTransactionFixture();
        var command = fixture.Command with
        {
            Method = PaymentMethod.BankTransfer,
            TransferAccountId = null
        };

        var result = await fixture.Validator.TestValidateAsync(
            command,
            cancellationToken: fixture.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.TransferAccountId)
            .WithErrorCode(ErrorCodes.TransactionTransferAccountRequired);
    }

    [Fact]
    public async Task UpdateTransactionValidator_WhenTransactionBelongsToAnotherBudget_ReturnsExpectedError()
    {
        var fixture = CreateUpdateTransactionFixture();
        var otherBudget = Budget.Create("Other", Guid.NewGuid());

        fixture.BudgetContext
            .GetAsync(fixture.Command.BudgetId, fixture.CancellationToken)
            .Returns(otherBudget);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        Assert.Contains(
            result.Errors,
            x => x.PropertyName == nameof(UpdateTransactionCommand.TransactionId) &&
                 x.ErrorCode == ErrorCodes.TransactionNotExistsInBudget);
    }


    [Fact]
    public async Task CreateBankValidator_WhenBicFormatIsInvalid_UsesNamespacedDomainErrorCode()
    {
        var repository = Substitute.For<IBankRepository>();
        repository.IsNameUniqueAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(true);

        var validator = new CreateBankCommandValidator(repository);

        var result = await validator.TestValidateAsync(
            new CreateBankCommand("Bank", "BAD"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(
            result.Errors,
            x => x.PropertyName == nameof(CreateBankCommand.Bic) &&
                 x.ErrorCode == ErrorCodes.BankBicInvalidCharsCount);
    }

    [Fact]
    public async Task CreateAccountValidator_WhenIbanFormatIsInvalid_UsesNamespacedDomainErrorCode()
    {
        var repository = Substitute.For<IAccountRepository>();
        var bankContext = Substitute.For<IBankContext>();
        var bankId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository.IsNameUniqueAsync(Arg.Any<string>(), null, cancellationToken).Returns(true);
        bankContext.ExistsAsync(bankId, cancellationToken).Returns(true);

        var validator = new CreateAccountCommandValidator(repository, bankContext);

        var result = await validator.TestValidateAsync(
            new CreateAccountCommand("Account", "BAD", bankId),
            cancellationToken: cancellationToken);

        Assert.Contains(
            result.Errors,
            x => x.PropertyName == nameof(CreateAccountCommand.Iban) &&
                 x.ErrorCode == ErrorCodes.AccountIbanFrenchOnly);
    }

    [Fact]
    public void PublicErrorCodes_HaveExpectedStableValues()
    {
        Assert.Equal("Bank.{0}", ErrorCodes.BankErrorCodeFormat);
        Assert.Equal("Account.{0}", ErrorCodes.AccountErrorCodeFormat);
        Assert.Equal("BudgetCategory.NotExists", ErrorCodes.BudgetCategoryNotExists);
        Assert.Equal("Budget.Category.NotExists", ErrorCodes.BudgetBudgetCategoryNotExists);
        Assert.Equal("Search.SortDirection.Invalid", ErrorCodes.SearchSortDirectionInvalid);
        Assert.Equal("Search.SortField.Invalid", ErrorCodes.SearchSortFieldInvalid);
    }

    private static UpdateTransactionFixture CreateUpdateTransactionFixture()
    {
        var budgetContext = Substitute.For<IBudgetContext>();
        var accountContext = Substitute.For<IAccountContext>();
        var transactionContext = Substitute.For<ITransactionContext>();
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerId = Guid.NewGuid();
        var category = BudgetCategory.Create("Category", null);
        var budget = Budget.Create("Budget", ownerId);
        var accountId = Guid.NewGuid();

        budget.AssociateCategory(category.Id, ownerId);
        var transaction = budget.AddTransaction(
            category.Id,
            accountId,
            "Operation",
            TransactionType.Expense,
            10m,
            PaymentMethod.Cash,
            null,
            ownerId);

        budgetContext.ExistsAsync(budget.Id, cancellationToken).Returns(true);
        budgetContext.IsEditableAsync(budget.Id, cancellationToken).Returns(BudgetEditableStatus.Editable);
        budgetContext.GetAsync(budget.Id, cancellationToken).Returns(budget);

        transactionContext.ExistsAsync(transaction.Id, cancellationToken).Returns(true);
        transactionContext.GetAsync(transaction.Id, cancellationToken).Returns(transaction);

        transactionRepository
            .IsNameUniqueAsync(
                Arg.Any<string>(),
                budget.Id,
                category.Id,
                transaction.Id,
                cancellationToken)
            .Returns(true);

        var validator = new UpdateTransactionCommandValidator(
            budgetContext,
            accountContext,
            transactionContext,
            transactionRepository);

        var command = new UpdateTransactionCommand(
            budget.Id,
            transaction.Id,
            "Updated operation",
            20m,
            PaymentMethod.Cash,
            null);

        return new UpdateTransactionFixture(
            validator,
            budgetContext,
            command,
            cancellationToken);
    }

    private sealed record UpdateTransactionFixture(
        UpdateTransactionCommandValidator Validator,
        IBudgetContext BudgetContext,
        UpdateTransactionCommand Command,
        CancellationToken CancellationToken);


    [Fact]
    public async Task ReorderCategoriesValidator_WhenCommandIsValid_HasNoErrors()
    {
        var budgetContext = Substitute.For<IBudgetContext>();
        var categoryContext = Substitute.For<IBudgetCategoryContext>();
        var budgetId = Guid.NewGuid();
        var firstCategoryId = Guid.NewGuid();
        var secondCategoryId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        budget.AssociateCategory(firstCategoryId, ownerId);
        budget.AssociateCategory(secondCategoryId, ownerId);
        var cancellationToken = TestContext.Current.CancellationToken;
        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsEditableAsync(budgetId, cancellationToken).Returns(BudgetEditableStatus.Editable);
        budgetContext.GetAsync(budgetId, cancellationToken).Returns(budget);
        categoryContext.ExistsAsync(firstCategoryId, cancellationToken).Returns(true);
        categoryContext.ExistsAsync(secondCategoryId, cancellationToken).Returns(true);
        categoryContext.GetAsync(firstCategoryId, cancellationToken).Returns(BudgetCategory.Create("First", null));
        categoryContext.GetAsync(secondCategoryId, cancellationToken).Returns(BudgetCategory.Create("Second", null));
        categoryContext.IsAssociatedToBudgetAsync(firstCategoryId, budgetId, cancellationToken).Returns(true);
        categoryContext.IsAssociatedToBudgetAsync(secondCategoryId, budgetId, cancellationToken).Returns(true);
        var validator = new ReorderCategoriesCommandValidator(budgetContext, categoryContext);

        var result = await validator.TestValidateAsync(
            new ReorderCategoriesCommand(budgetId, [firstCategoryId, secondCategoryId]),
            cancellationToken: cancellationToken);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ReorderCategoriesValidator_WhenCategoryDoesNotExist_DoesNotCheckAssociationForIt()
    {
        var budgetContext = Substitute.For<IBudgetContext>();
        var categoryContext = Substitute.For<IBudgetCategoryContext>();
        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var cancellationToken = TestContext.Current.CancellationToken;
        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsEditableAsync(budgetId, cancellationToken).Returns(BudgetEditableStatus.Editable);
        budgetContext.GetAsync(budgetId, cancellationToken).Returns(budget);
        categoryContext.ExistsAsync(categoryId, cancellationToken).Returns(false);
        categoryContext.GetAsync(categoryId, cancellationToken).Returns((BudgetCategory?)null);
        var validator = new ReorderCategoriesCommandValidator(budgetContext, categoryContext);

        var result = await validator.TestValidateAsync(
            new ReorderCategoriesCommand(budgetId, [categoryId]),
            cancellationToken: cancellationToken);

        Assert.Contains(result.Errors, x => x.ErrorCode == ErrorCodes.BudgetBudgetCategoryNotExists);
        await categoryContext.DidNotReceive().IsAssociatedToBudgetAsync(
            categoryId,
            budgetId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderCategoriesValidator_WhenCategoryIsNotAssociated_ReturnsExpectedError()
    {
        var budgetContext = Substitute.For<IBudgetContext>();
        var categoryContext = Substitute.For<IBudgetCategoryContext>();
        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);
        var cancellationToken = TestContext.Current.CancellationToken;
        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsEditableAsync(budgetId, cancellationToken).Returns(BudgetEditableStatus.Editable);
        budgetContext.GetAsync(budgetId, cancellationToken).Returns(budget);
        categoryContext.ExistsAsync(categoryId, cancellationToken).Returns(true);
        categoryContext.GetAsync(categoryId, cancellationToken).Returns(category);
        categoryContext.IsAssociatedToBudgetAsync(categoryId, budgetId, cancellationToken).Returns(false);
        var validator = new ReorderCategoriesCommandValidator(budgetContext, categoryContext);

        var result = await validator.TestValidateAsync(
            new ReorderCategoriesCommand(budgetId, [categoryId]),
            cancellationToken: cancellationToken);

        Assert.Contains(result.Errors, x => x.ErrorCode == ErrorCodes.BudgetBudgetCategoryNotAssociated);
    }

}

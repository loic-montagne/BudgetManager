using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Features.Account.Close;
using BudgetManager.Application.Features.Account.Create;
using BudgetManager.Application.Features.Account.Delete;
using BudgetManager.Application.Features.Account.GetById;
using BudgetManager.Application.Features.Account.Search;
using BudgetManager.Application.Features.Account.Update;
using BudgetManager.Application.Features.Bank.Create;
using BudgetManager.Application.Features.Bank.Delete;
using BudgetManager.Application.Features.Bank.GetById;
using BudgetManager.Application.Features.Bank.Search;
using BudgetManager.Application.Features.Bank.Update;
using BudgetManager.Application.Features.Budget.Create;
using BudgetManager.Application.Features.Budget.Delete;
using BudgetManager.Application.Features.Budget.GetAccesses;
using BudgetManager.Application.Features.Budget.GetById;
using BudgetManager.Application.Features.Budget.Lock;
using BudgetManager.Application.Features.Budget.Search;
using BudgetManager.Application.Features.Budget.Unlock;
using BudgetManager.Application.Features.Budget.Update;
using BudgetManager.Application.Features.BudgetAccess.GetByKey;
using BudgetManager.Application.Features.BudgetCategory.Create;
using BudgetManager.Application.Features.BudgetCategory.Delete;
using BudgetManager.Application.Features.BudgetCategory.GetById;
using BudgetManager.Application.Features.BudgetCategory.Search;
using BudgetManager.Application.Features.BudgetCategory.Update;
using BudgetManager.Application.Features.Transaction.Delete;
using BudgetManager.Application.Features.Transaction.GetById;
using BudgetManager.Domain.ValueObjects;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;
using AccountPagedSearchCriteria = BudgetManager.Application.Features.Account.Search.PagedSearchCriteria;
using BudgetPagedSearchCriteria = BudgetManager.Application.Features.Budget.Search.PagedSearchCriteria;

namespace BudgetManager.Application.Tests;

public sealed class ValidatorTests
{
    [Fact]
    public async Task CloseAccountValidator_WhenAccountIsOpen_HasNoErrors()
    {
        // Arrange

        var accountContext = Substitute.For<IAccountContext>();
        var accountId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        accountContext.ExistsAsync(accountId, cancellationToken).Returns(true);
        accountContext.IsOpenedAsync(accountId, cancellationToken).Returns(true);

        var validator = new CloseAccountCommandValidator(accountContext);
        var command = new CloseAccountCommand(accountId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task CloseAccountValidator_WhenAccountIsClosed_ReturnsExpectedError()
    {
        // Arrange

        var accountContext = Substitute.For<IAccountContext>();
        var accountId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        accountContext.ExistsAsync(accountId, cancellationToken).Returns(true);
        accountContext.IsOpenedAsync(accountId, cancellationToken).Returns(false);

        var validator = new CloseAccountCommandValidator(accountContext);
        var command = new CloseAccountCommand(accountId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(item => item.Id)
            .WithErrorCode(ErrorCodes.AccountIsAlreadyClosed);
    }

    [Fact]
    public async Task CreateAccountValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var accountRepository = Substitute.For<IAccountRepository>();
        var bankContext = Substitute.For<IBankContext>();
        var bankId = Guid.NewGuid();
        var iban = "FR7630006000011234567890189";
        var cancellationToken = TestContext.Current.CancellationToken;

        accountRepository.IsNameUniqueAsync("Account", null, cancellationToken).Returns(true);
        accountRepository.IsIbanUniqueAsync(Iban.Create(iban), null, cancellationToken).Returns(true);
        bankContext.ExistsAsync(bankId, cancellationToken).Returns(true);

        var validator = new CreateAccountCommandValidator(accountRepository, bankContext);
        var command = new CreateAccountCommand(" Account ", iban, bankId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DeleteAccountValidator_WhenAccountIsUsed_ReturnsExpectedError()
    {
        // Arrange

        var accountContext = Substitute.For<IAccountContext>();
        var accountRepository = Substitute.For<IAccountRepository>();
        var accountId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        accountContext.ExistsAsync(accountId, cancellationToken).Returns(true);
        accountRepository.IsUsedAsync(accountId, cancellationToken).Returns(true);

        var validator = new DeleteAccountCommandValidator(accountContext, accountRepository);
        var command = new DeleteAccountCommand(accountId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(item => item.Id)
            .WithErrorCode(ErrorCodes.AccountIsUsed);
    }

    [Fact]
    public async Task UpdateAccountValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var accountContext = Substitute.For<IAccountContext>();
        var accountRepository = Substitute.For<IAccountRepository>();
        var accountId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        accountContext.ExistsAsync(accountId, cancellationToken).Returns(true);
        accountContext.IsOpenedAsync(accountId, cancellationToken).Returns(true);
        accountRepository.IsNameUniqueAsync("Account", accountId, cancellationToken).Returns(true);

        var validator = new UpdateAccountCommandValidator(accountContext, accountRepository);
        var command = new UpdateAccountCommand(accountId, " Account ");

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task CreateBankValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var bankRepository = Substitute.For<IBankRepository>();
        var bic = "BNPAFRPP";
        var cancellationToken = TestContext.Current.CancellationToken;

        bankRepository.IsNameUniqueAsync("Bank", null, cancellationToken).Returns(true);
        bankRepository.IsBicUniqueAsync(Bic.Create(bic), null, cancellationToken).Returns(true);

        var validator = new CreateBankCommandValidator(bankRepository);
        var command = new CreateBankCommand(" Bank ", bic);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DeleteBankValidator_WhenBankIsUsed_ReturnsExpectedError()
    {
        // Arrange

        var bankContext = Substitute.For<IBankContext>();
        var bankRepository = Substitute.For<IBankRepository>();
        var bankId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        bankContext.ExistsAsync(bankId, cancellationToken).Returns(true);
        bankRepository.IsUsedAsync(bankId, cancellationToken).Returns(true);

        var validator = new DeleteBankCommandValidator(bankContext, bankRepository);
        var command = new DeleteBankCommand(bankId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(item => item.Id)
            .WithErrorCode(ErrorCodes.BankIsUsed);
    }

    [Fact]
    public async Task UpdateBankValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var bankContext = Substitute.For<IBankContext>();
        var bankRepository = Substitute.For<IBankRepository>();
        var bankId = Guid.NewGuid();
        var bic = "BNPAFRPP";
        var cancellationToken = TestContext.Current.CancellationToken;

        bankContext.ExistsAsync(bankId, cancellationToken).Returns(true);
        bankRepository.IsNameUniqueAsync("Bank", bankId, cancellationToken).Returns(true);
        bankRepository.IsBicUniqueAsync(Bic.Create(bic), bankId, cancellationToken).Returns(true);

        var validator = new UpdateBankCommandValidator(bankContext, bankRepository);
        var command = new UpdateBankCommand(bankId, " Bank ", bic);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task CreateBudgetValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var budgetRepository = Substitute.For<IBudgetRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetRepository.IsNameUniqueAsync("Budget", null, cancellationToken).Returns(true);

        var validator = new CreateBudgetCommandValidator(budgetRepository);
        var command = new CreateBudgetCommand(" Budget ");

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateBudgetValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var budgetRepository = Substitute.For<IBudgetRepository>();
        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsEditableAsync(budgetId, cancellationToken).Returns(BudgetEditableStatus.Editable);
        budgetRepository.IsNameUniqueAsync("Budget", budgetId, cancellationToken).Returns(true);

        var validator = new UpdateBudgetCommandValidator(budgetRepository, budgetContext);
        var command = new UpdateBudgetCommand(budgetId, " Budget ");

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DeleteBudgetValidator_WhenCurrentUserIsNotOwner_ReturnsExpectedError()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var currentUserId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var currentUser = new TestCurrentUser(true, currentUserId);
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsOwnerAsync(budgetId, currentUserId, cancellationToken).Returns(false);

        var validator = new DeleteBudgetCommandValidator(budgetContext, currentUser);
        var command = new DeleteBudgetCommand(budgetId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(item => item.Id)
            .WithErrorCode(ErrorCodes.BudgetPermissionInvalid);
    }

    [Theory]
    [InlineData(BudgetLockableStatus.AlreadyLocked, ErrorCodes.BudgetIsLocked)]
    [InlineData(BudgetLockableStatus.NotAuthorized, ErrorCodes.BudgetPermissionInvalid)]
    public async Task LockBudgetValidator_WhenBudgetCannotBeLocked_ReturnsExpectedError(
        BudgetLockableStatus status,
        string errorCode)
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsLockableAsync(budgetId, cancellationToken).Returns(status);

        var validator = new LockBudgetCommandValidator(budgetContext);
        var command = new LockBudgetCommand(budgetId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(item => item.Id)
            .WithErrorCode(errorCode);
    }

    [Fact]
    public async Task LockBudgetValidator_WhenBudgetIsLockable_HasNoErrors()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsLockableAsync(budgetId, cancellationToken).Returns(BudgetLockableStatus.Lockable);

        var validator = new LockBudgetCommandValidator(budgetContext);
        var command = new LockBudgetCommand(budgetId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(BudgetUnlockableStatus.AlreadyUnlocked, ErrorCodes.BudgetIsUnlocked)]
    [InlineData(BudgetUnlockableStatus.NotAuthorized, ErrorCodes.BudgetPermissionInvalid)]
    public async Task UnlockBudgetValidator_WhenBudgetCannotBeUnlocked_ReturnsExpectedError(
        BudgetUnlockableStatus status,
        string errorCode)
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsUnlockableAsync(budgetId, cancellationToken).Returns(status);

        var validator = new UnlockBudgetCommandValidator(budgetContext);
        var command = new UnlockBudgetCommand(budgetId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(item => item.Id)
            .WithErrorCode(errorCode);
    }

    [Fact]
    public async Task UnlockBudgetValidator_WhenBudgetIsUnlockable_HasNoErrors()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsUnlockableAsync(budgetId, cancellationToken).Returns(BudgetUnlockableStatus.Unlockable);

        var validator = new UnlockBudgetCommandValidator(budgetContext);
        var command = new UnlockBudgetCommand(budgetId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task CreateBudgetCategoryValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var repository = Substitute.For<IBudgetCategoryRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository.IsNameUniqueAsync("Category", null, cancellationToken).Returns(true);

        var validator = new CreateBudgetCategoryCommandValidator(repository);
        var command = new CreateBudgetCategoryCommand(" Category ", "Description");

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateBudgetCategoryValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var context = Substitute.For<IBudgetCategoryContext>();
        var repository = Substitute.For<IBudgetCategoryRepository>();
        var categoryId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        context.ExistsAsync(categoryId, cancellationToken).Returns(true);
        repository.IsNameUniqueAsync("Category", categoryId, cancellationToken).Returns(true);

        var validator = new UpdateBudgetCategoryValidator(context, repository);
        var command = new UpdateBudgetCategoryCommand(categoryId, " Category ", "Description");

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DeleteBudgetCategoryValidator_WhenCategoryIsUsed_ReturnsExpectedError()
    {
        // Arrange

        var context = Substitute.For<IBudgetCategoryContext>();
        var repository = Substitute.For<IBudgetCategoryRepository>();
        var categoryId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        context.ExistsAsync(categoryId, cancellationToken).Returns(true);
        repository.IsUsedAsync(categoryId, cancellationToken).Returns(true);

        var validator = new DeleteBudgetCategoryCommandValidator(context, repository);
        var command = new DeleteBudgetCategoryCommand(categoryId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(item => item.Id)
            .WithErrorCode(ErrorCodes.BudgetCategoryIsUsed);
    }

    [Fact]
    public async Task DeleteTransactionValidator_WhenTransactionIsOutsideBudget_ReturnsExpectedError()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var transactionContext = Substitute.For<ITransactionContext>();
        var ownerId = Guid.NewGuid();
        var budget = BudgetManager.Domain.Entities.Budget.Create("Budget", ownerId);
        var otherBudget = BudgetManager.Domain.Entities.Budget.Create("Other", ownerId);
        var category = BudgetManager.Domain.Entities.BudgetCategory.Create("Category", null);
        otherBudget.AssociateCategory(category, ownerId);
        var transaction = otherBudget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Transaction",
            BudgetManager.Domain.Enums.TransactionType.Expense,
            10,
            BudgetManager.Domain.Enums.PaymentMethod.Cash,
            null,
            ownerId);
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budget.Id, cancellationToken).Returns(true);
        budgetContext.IsEditableAsync(budget.Id, cancellationToken).Returns(BudgetEditableStatus.Editable);
        budgetContext.GetAsync(budget.Id, cancellationToken).Returns(budget);
        transactionContext.ExistsAsync(transaction.Id, cancellationToken).Returns(true);
        transactionContext.GetAsync(transaction.Id, cancellationToken).Returns(transaction);

        var validator = new DeleteTransactionCommandValidator(budgetContext, transactionContext);
        var command = new DeleteTransactionCommand(budget.Id, transaction.Id);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(item => item.TransactionId)
            .WithErrorCode(ErrorCodes.TransactionNotExistsInBudget);
    }

    [Theory]
    [InlineData("account")]
    [InlineData("bank")]
    [InlineData("budget")]
    [InlineData("category")]
    public async Task SearchValidators_WhenPaginationIsInvalid_ReturnPaginationErrors(string validatorType)
    {
        // Arrange

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act / Assert

        switch (validatorType)
        {
            case "account":
            {
                var validator = new SearchAccountsQueryValidator();
                var command = new SearchAccountsQuery(new AccountPagedSearchCriteria(null, true, [], null, -1, 0, null));
                var result = await validator.TestValidateAsync(command, cancellationToken: cancellationToken);
                Assert.False(result.IsValid);
                break;
            }
            case "bank":
            {
                var validator = new SearchBanksQueryValidator();
                var command = new SearchBanksQuery(new PagedSearchCriteria<BankSortField>(null, -1, 0, null));
                var result = await validator.TestValidateAsync(command, cancellationToken: cancellationToken);
                Assert.False(result.IsValid);
                break;
            }
            case "budget":
            {
                var validator = new SearchBudgetsQueryValidator();
                var command = new SearchBudgetsQuery(new BudgetPagedSearchCriteria(null, null, -1, 0, null));
                var result = await validator.TestValidateAsync(command, cancellationToken: cancellationToken);
                Assert.False(result.IsValid);
                break;
            }
            default:
            {
                var validator = new SearchBudgetCategoriesQueryValidator();
                var command = new SearchBudgetCategoriesQuery(new PagedSearchCriteria<BudgetCategorySortField>(null, -1, 0, null));
                var result = await validator.TestValidateAsync(command, cancellationToken: cancellationToken);
                Assert.False(result.IsValid);
                break;
            }
        }
    }

    [Theory]
    [InlineData("account")]
    [InlineData("bank")]
    [InlineData("budget")]
    [InlineData("budgetAccess")]
    [InlineData("category")]
    [InlineData("transaction")]
    public async Task GetByIdValidators_WhenIdentifierIsEmpty_ReturnExpectedError(string validatorType)
    {
        // Arrange

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act / Assert

        switch (validatorType)
        {
            case "account":
            {
                var result = await new GetAccountByIdQueryValidator().TestValidateAsync(
                    new GetAccountByIdQuery(Guid.Empty),
                    cancellationToken: cancellationToken);
                result.ShouldHaveValidationErrorFor(item => item.Id);
                break;
            }
            case "bank":
            {
                var result = await new GetBankByIdQueryValidator().TestValidateAsync(
                    new GetBankByIdQuery(Guid.Empty),
                    cancellationToken: cancellationToken);
                result.ShouldHaveValidationErrorFor(item => item.Id);
                break;
            }
            case "budget":
            {
                var result = await new GetBudgetByIdQueryValidator().TestValidateAsync(
                    new GetBudgetByIdQuery(Guid.Empty),
                    cancellationToken: cancellationToken);
                result.ShouldHaveValidationErrorFor(item => item.Id);
                break;
            }
            case "budgetAccess":
            {
                var result = await new GetBudgetAccessByKeyQueryValidator().TestValidateAsync(
                    new GetBudgetAccessByKeyQuery(Guid.Empty, Guid.Empty),
                    cancellationToken: cancellationToken);
                result.ShouldHaveValidationErrorFor(item => item.BudgetId);
                result.ShouldHaveValidationErrorFor(item => item.UserId);
                break;
            }
            case "category":
            {
                var result = await new GetBudgetCategoryByIdQueryValidator().TestValidateAsync(
                    new GetBudgetCategoryByIdQuery(Guid.Empty),
                    cancellationToken: cancellationToken);
                result.ShouldHaveValidationErrorFor(item => item.Id);
                break;
            }
            default:
            {
                var result = await new GetTransactionByIdQueryValidator().TestValidateAsync(
                    new GetTransactionByIdQuery(Guid.Empty),
                    cancellationToken: cancellationToken);
                result.ShouldHaveValidationErrorFor(item => item.Id);
                break;
            }
        }
    }

    [Fact]
    public async Task GetBudgetAccessesValidator_WhenIdentifierIsEmpty_ReturnsExpectedError()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var validator = new GetBudgetAccessesQueryValidator(budgetContext);
        var query = new GetBudgetAccessesQuery(Guid.Empty);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var result = await validator.TestValidateAsync(
            query,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(item => item.Id)
            .WithErrorCode(ErrorCodes.BudgetIdRequired);
    }
}

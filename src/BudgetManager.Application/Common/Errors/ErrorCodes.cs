namespace BudgetManager.Application.Common.Errors;

public static class ErrorCodes
{
    // Culture
    public const string CultureNotSupported = "Culture.NotSupported";

    // Theme
    public const string ThemeNotSupported = "Theme.NotSupported";

    // Token
    public const string TokenRequired = "Token.Required";

    // Search params
    public const string SearchPaginationInvalid = "Search.Pagination.Invalid";
    public const string SearchOffsetInvalid = "Search.Offset.Invalid";
    public const string SearchLimitInvalid = "Search.Limit.Invalid";
    public const string SearchSortDirectionInvalid = "Search.SortDirection.Invalid";
    public const string SearchSortFieldInvalid = "Search.SortField.Invalid";

    // Bank entity
    internal const string BankErrorCodeFormat = "Bank.{0}";
    public const string BankNotExists = "Bank.NotExists";
    public const string BankIsUsed = "Bank.IsUsed";
    public const string BankIdRequired = "Bank.Id.Required";
    public const string BankNameRequired = "Bank.Name.Required";
    public const string BankNameTooLong = "Bank.Name.TooLong";
    public const string BankNameAlreadyUsed = "Bank.Name.AlreadyUsed";
    public const string BankBicRequired = "Bank.Bic.Required";
    public const string BankBicInvalidChars = $"Bank.{Domain.Common.ErrorCodes.BicInvalidChars}";
    public const string BankBicInvalidCharsCount = $"Bank.{Domain.Common.ErrorCodes.BicInvalidCharsCount}";
    public const string BankBicBankCodeInvalidChars = $"Bank.{Domain.Common.ErrorCodes.BicBankCodeInvalidChars}";
    public const string BankBicFrenchOnly = $"Bank.{Domain.Common.ErrorCodes.BicFrenchOnly}";
    public const string BankBicAlreadyUsed = "Bank.Bic.AlreadyUsed";

    // Account entity
    internal const string AccountErrorCodeFormat = "Account.{0}";
    public const string AccountNotExists = "Account.NotExists";
    public const string AccountIsUsed = "Account.IsUsed";
    public const string AccountIsAlreadyClosed = "Account.IsAlreadyClosed";
    public const string AccountIdRequired = "Account.Id.Required";
    public const string AccountNameRequired = "Account.Name.Required";
    public const string AccountNameTooLong = "Account.Name.TooLong";
    public const string AccountNameAlreadyUsed = "Account.Name.AlreadyUsed";
    public const string AccountBankRequired = "Account.Bank.Required";
    public const string AccountBankNotExists = "Account.Bank.NotExists";
    public const string AccountIbanRequired = "Account.Iban.Required";
    public const string AccountIbanInvalidChars = $"Account.{Domain.Common.ErrorCodes.IbanInvalidChars}";
    public const string AccountIbanFrenchOnly = $"Account.{Domain.Common.ErrorCodes.IbanFrenchOnly}";
    public const string AccountIbanInvalidCharsCount = $"Account.{Domain.Common.ErrorCodes.IbanInvalidCharsCount}";
    public const string AccountIbanInvalidChecksum = $"Account.{Domain.Common.ErrorCodes.IbanInvalidChecksum}";
    public const string AccountIbanAlreadyUsed = "Account.Iban.AlreadyUsed";

    // Budget category entity
    public const string BudgetCategoryNotExists = "BudgetCategory.NotExists";
    public const string BudgetCategoryIsUsed = "BudgetCategory.IsUsed";
    public const string BudgetCategoryIdRequired = "BudgetCategory.Id.Required";
    public const string BudgetCategoryNameRequired = "BudgetCategory.Name.Required";
    public const string BudgetCategoryNameTooLong = "BudgetCategory.Name.TooLong";
    public const string BudgetCategoryNameAlreadyUsed = "BudgetCategory.Name.AlreadyUsed";
    public const string BudgetCategoryDescriptionTooLong = "BudgetCategory.Description.TooLong";

    // Budget access entity
    public const string BudgetAccessNotExists = "BudgetAccess.NotExists";
    public const string BudgetAccessBudgetIdRequired = "BudgetAccess.BudgetId.Required";
    public const string BudgetAccessUserIdRequired = "BudgetAccess.UserId.Required";
    public const string BudgetAccessUserIdNotExists = "BudgetAccess.UserId.NotExists";

    // Budget entity
    public const string BudgetNotExists = "Budget.NotExists";
    public const string BudgetPermissionInvalid = "Budget.Permission.Invalid";
    public const string BudgetIsLocked = "Budget.IsLocked";
    public const string BudgetIsUnlocked = "Budget.IsUnlocked";
    public const string BudgetIdRequired = "Budget.Id.Required";
    public const string BudgetNameRequired = "Budget.Name.Required";
    public const string BudgetNameTooLong = "Budget.Name.TooLong";
    public const string BudgetNameAlreadyUsed = "Budget.Name.AlreadyUsed";
    public const string BudgetUserRequired = "Budget.User.Required";
    public const string BudgetUserNotExists = "Budget.User.NotExists";
    public const string BudgetUserIsCurrent = "Budget.User.IsCurrent";
    public const string BudgetUserIsOwner = "Budget.User.IsOwner";
    public const string BudgetUserIsNotOwner = "Budget.User.IsNotOwner";
    public const string BudgetPermissionsInvalid = "Budget.Permissions.Invalid";
    public const string BudgetBudgetCategoryRequired = "Budget.Category.Required";
    public const string BudgetBudgetCategoryNotExists = "Budget.Category.NotExists";
    public const string BudgetBudgetCategoryAssociated = "Budget.Category.Associated";
    public const string BudgetBudgetCategoryNotAssociated = "Budget.Category.NotAssociated";
    public const string BudgetBudgetCategoryIsUsedInBudget = "Budget.Category.IsUsed";

    // Transaction entity
    public const string TransactionNotExists = "Transaction.NotExists";
    public const string TransactionNotExistsInBudget = "Transaction.NotExistsInBudget";
    public const string TransactionIdRequired = "Transaction.Id.Required";
    public const string TransactionCategoryRequired = "Transaction.Category.Required";
    public const string TransactionCategoryNotExists = "Transaction.Category.NotExists";
    public const string TransactionCategoryNotAssociatedWithBudget = "Transaction.Category.NotAssociatedWithBudget";
    public const string TransactionNameRequired = "Transaction.Name.Required";
    public const string TransactionNameTooLong = "Transaction.Name.TooLong";
    public const string TransactionNameAlreadyUsed = "Transaction.Name.AlreadyUsed";
    public const string TransactionTypeInvalid = "Transaction.Type.Invalid";
    public const string TransactionMethodInvalid = "Transaction.Method.Invalid";
    public const string TransactionAccountRequired = "Transaction.Account.Required";
    public const string TransactionAccountNotExists = "Transaction.Account.NotExists";
    public const string TransactionAccountIsClosed = "Transaction.Account.IsClosed";
    public const string TransactionTransferAccountRequired = "Transaction.TransferAccount.Required";
    public const string TransactionTransferAccountNotExists = "Transaction.TransferAccount.NotExists";
    public const string TransactionTransferAccountIsClosed = "Transaction.TransferAccount.IsClosed";
    public const string TransactionTransferAccountEqualToAccount = "Transaction.TransferAccount.EqualToAccount";
    public const string TransactionAmountInvalid = "Transaction.Amount.Invalid";

    // User entity
    public const string UserIsLastActivatedAdministrator = "User.IsLastActivatedAdministrator";
    public const string UserIsCurrentUser = "User.IsCurrentUser";
    public const string UserIsBudgetOwner = "User.IsBudgetOwner";
    public const string UserNotExists = "User.NotExists";
    public const string UserNotCurrent = "User.NotCurrent";
    public const string UserIdRequired = "User.Id.Required";

    public const string UserActivationTokenRequired = "User.ActivationToken.Required";
    public const string UserActivationTokenLifetimeInvalid = "User.ActivationToken.Lifetime.Invalid";
    public const string UserActivationEmailSubjectRequired = "User.ActivationMail.Subject.Required";
    public const string UserActivationPageNameRequired = "User.ActivationPageName.Required";

    public const string UserEmailRequired = "User.Email.Required";
    public const string UserEmailTooLong = "User.Email.TooLong";
    public const string UserEmailInvalid = "User.Email.Invalid";
    public const string UserEmailInvalidCharacters = "User.Email.InvalidCharacters";
    public const string UserEmailAlreadyUsed = "User.Email.AlreadyUsed";
    public const string UserEmailAlreadyConfirmed = "User.Email.AlreadyConfirmed";
    public const string UserEmailNotConfirmed = "User.Email.NotConfirmed";

    public const string UserOldEmailRequired = "User.OldEmail.Required";
    public const string UserOldEmailNotCurrent = "User.OldEmail.NotCurrent";

    public const string UserNewEmailRequired = "User.NewEmail.Required";
    public const string UserNewEmailEqualsToOldEmail = "User.NewEmail.EqualsToOldEmail";
    public const string UserNewEmailTooLong = "User.NewEmail.TooLong";
    public const string UserNewEmailInvalid = "User.NewEmail.Invalid";
    public const string UserNewEmailInvalidCharacters = "User.NewEmail.InvalidCharacters";
    public const string UserNewEmailAlreadyUsed = "User.NewEmail.AlreadyUsed";

    public const string UserLastNameRequired = "User.LastName.Required";
    public const string UserLastNameTooLong = "User.LastName.TooLong";

    public const string UserFirstNameRequired = "User.FirstName.Required";
    public const string UserFirstNameTooLong = "User.FirstName.TooLong";

    public const string UserPhoneNumberTooLong = "User.PhoneNumber.TooLong";
    public const string UserPhoneNumberInvalid = "User.PhoneNumber.Invalid";

    public const string UserPasswordRequired = "User.Password.Required";
    public const string UserPasswordInvalid = "User.Password.Invalid";
    public const string UserPasswordNotEqualToConfirm = "User.Password.NotEqualToConfirm";

    public const string UserRoleRequired = "User.Role.Required";
    public const string UserRoleDuplicated = "User.Role.Duplicated";
    public const string UserRoleNotExists = "User.Role.NotExists";

    public const string UserProfilePictureRequired = "User.ProfilePicture.Required";
    public const string UserProfilePictureContentRequired = "User.ProfilePicture.Content.Required";
    public const string UserProfilePictureContentTypeRequired = "User.ProfilePicture.ContentType.Required";
    public const string UserProfilePictureNotExists = "User.ProfilePicture.NotExists";
    public const string UserProfilePictureTooLarge = "User.ProfilePicture.TooLarge";
    public const string UserProfilePictureFormatUnsupported = "User.ProfilePicture.FormatUnsupported";
    public const string UserProfilePictureInvalid = "User.ProfilePicture.Invalid";
    public const string UserProfilePictureDimensionsTooLarge = "User.ProfilePicture.DimensionTooLarge";
    public const string UserProfilePictureDimensionsTooSmall = "User.ProfilePicture.DimensionTooSmall";

}

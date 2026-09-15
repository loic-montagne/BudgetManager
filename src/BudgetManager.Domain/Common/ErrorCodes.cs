namespace BudgetManager.Domain.Common;

public static class ErrorCodes
{
    public const string BicInvalidChars = "Bic.InvalidChars";
    public const string BicInvalidCharsCount = "BicInvalidCharsCount";
    public const string BicBankCodeInvalidChars = "Bic.BankCodeInvalidChars";
    public const string BicFrenchOnly = "Bic.FrenchOnly";

    public const string IbanInvalidChars = "Iban.InvalidChars";
    public const string IbanFrenchOnly = "Iban.FrenchOnly";
    public const string IbanInvalidCharsCount = "Iban.InvalidCharsCount";
    public const string IbanInvalidChecksum = "Iban.InvalidChecksum";
}

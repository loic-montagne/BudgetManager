using BudgetManager.Application.Common;
using BudgetManager.Domain.Entities.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace BudgetManager.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>, IAuditable
{
    [ProtectedPersonalData]
    public string LastName { get; set; } = string.Empty;
    [ProtectedPersonalData]
    public string FirstName { get; set; } = string.Empty;

    public string PreferredCulture { get; set; } = SupportedCultures.Default;
    public string? PreferredTheme { get; set; }

    public DateTimeOffset? ActivationEmailSentOn { get; set; }
    public DateTimeOffset? ActivationEmailExpiresOn { get; set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public DateTimeOffset UpdatedOn { get; private set; }

    public void MarkCreated(DateTimeOffset dateTime, Guid? userId)
    {
        if (CreatedOn != default)
            throw new InvalidOperationException("User is already marked as created.");

        CreatedOn = dateTime;
        CreatedBy = userId ?? Guid.Empty;
        UpdatedOn = dateTime;
        UpdatedBy = userId ?? Guid.Empty;
    }

    public void MarkUpdated(DateTimeOffset dateTime, Guid? userId)
    {
        if (CreatedOn == default)
            throw new InvalidOperationException("User has not been created.");

        ArgumentOutOfRangeException.ThrowIfLessThan(dateTime, UpdatedOn, nameof(dateTime));

        UpdatedOn = dateTime;
        if (userId.HasValue)
            UpdatedBy = userId.Value;
    }
}

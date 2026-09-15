using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BudgetManager.Infrastructure.Identity;

internal sealed class AccountActivationTokenProvider(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<AccountActivationTokenProviderOptions> options,
    ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
    : DataProtectorTokenProvider<ApplicationUser>(
        dataProtectionProvider,
        options,
        logger);

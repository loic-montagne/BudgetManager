using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BudgetManager.Infrastructure.Identity;

internal sealed class EmailChangeTokenProvider(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<EmailChangeTokenProviderOptions> options,
    ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
    : DataProtectorTokenProvider<ApplicationUser>(
        dataProtectionProvider,
        options,
        logger);

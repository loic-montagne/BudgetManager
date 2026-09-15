using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Email;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Abstractions.Localization;
using BudgetManager.Application.Email;
using BudgetManager.Application.Email.Templates;
using MediatR;
using System.Globalization;

namespace BudgetManager.Application.Features.User.SendActivationEmail;

public sealed class SendUserActivationEmailCommandHandler(IUserContext userContext, IUserManager userManager, IActivationUrlGenerator activationUrlGenerator, ITemplatedEmailSender emailSender, IDateTimeLocalizer dateTimeLocalizer, TimeProvider timeProvider) : IRequestHandler<SendUserActivationEmailCommand, DateTimeOffset>
{
    public async Task<DateTimeOffset> Handle(SendUserActivationEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userContext.GetRequiredAsync(request.Id, cancellationToken);
        var sentOn = timeProvider.GetUtcNow();
        var expiresOn = sentOn + request.TokenLifetime;
        var localExpiresOn = dateTimeLocalizer.ToLocalTime(expiresOn);
        var culture = CultureInfo.CurrentUICulture;
        var callbackUrl = await activationUrlGenerator.Generate(request.Id, request.ActivationPageName, request.RouteValues, cancellationToken);

        var message =
            new TemplatedEmailMessage
            {
                Subject = request.Subject,
                TemplateName = EmailTemplates.AccountActivation
            };

        message.To.Add(new EmailAddress(user.Email));

        await emailSender.SendAsync(
            message,
            new AccountActivationEmailModel(
                user.FirstName,
                callbackUrl,
                localExpiresOn.ToString("g", culture)),
            culture,
            cancellationToken);

        await userManager.SaveActivationEmailAsync(request.Id, sentOn, expiresOn, cancellationToken);

        return expiresOn;
    }
}

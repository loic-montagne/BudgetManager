using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.User.Common;
using BudgetManager.Application.Features.User.SendActivationEmail;
using MediatR;

namespace BudgetManager.Application.Features.User.Create;

public sealed class CreateUserCommandHandler(ISender sender, IUserManager userManager, IPasswordGenerator passwordGenerator, IProfilePictureProcessor profilePictureProcessor) : IRequestHandler<CreateUserCommand, (Guid id, DateTimeOffset expiresOn)>
{
    public async Task<(Guid id, DateTimeOffset expiresOn)> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        UserProfilePicture? processedPicture = null;
        if (request.ProfilePicture is not null)
        {
            var result = profilePictureProcessor.Process(request.ProfilePicture, cancellationToken);
            BadRequestException.ThrowIfResultIsNotValid(result, nameof(request.ProfilePicture));
            processedPicture = result.Value;
        }

        var userData = request.ToUserData();
        var password = passwordGenerator.Generate(20);
        var id = await userManager.CreateAsync(request.NormalizedEmail, userData, processedPicture, password, request.Roles, cancellationToken);
        var expiresOn = await sender.Send(new SendUserActivationEmailCommand(id, request.ActivationPageName, request.RouteValues, request.ActivationTokenLifetime, request.ActivationEmailSubject), cancellationToken);

        return (id, expiresOn);
    }
}

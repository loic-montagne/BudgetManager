namespace BudgetManager.Application.Features.User.GetById;

public sealed record ProfilePictureDto(byte[] Content, string ContentType, Guid CreatedBy, string CreatedByName, DateTimeOffset CreatedOn, Guid UpdatedBy, string UpdatedByName, DateTimeOffset UpdatedOn);
public sealed record CompleteUserDto(Guid Id, string UserName, string LastName, string FirstName, string Email, string? PhoneNumber, string PreferredCulture, string? PreferredTheme, bool EmailConfirmed, DateTimeOffset? ActivationEmailSentOn, DateTimeOffset? ActivationEmailExpiresOn, IReadOnlyCollection<string> Roles, ProfilePictureDto? ProfilePicture, Guid CreatedBy, string CreatedByName, DateTimeOffset CreatedOn, Guid UpdatedBy, string UpdatedByName, DateTimeOffset UpdatedOn);

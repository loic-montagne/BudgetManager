using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.User.Common;
using BudgetManager.Domain.Entities;
using BudgetManager.Infrastructure.Extensions;
using BudgetManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BudgetManager.Infrastructure.Identity;

internal sealed class UserManager(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager) : IUserManager
{
    public async Task<bool> RoleExistsAsync(string role, CancellationToken cancellationToken)
    {
        return await roleManager.RoleExistsAsync(role);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context
            .Set<ApplicationUser>()
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ProfilePictureExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context
            .Set<ApplicationUserProfilePicture>()
            .AnyAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludingId, CancellationToken cancellationToken)
    {
        return await context
            .Set<ApplicationUser>()
            .AnyAsync(x => x.Email == email && (excludingId == null || x.Id != excludingId), cancellationToken);
    }

    public async Task<bool> EmailIsCurrentAsync(Guid id, string email, CancellationToken cancellationToken)
    {
        return await context
            .Set<ApplicationUser>()
            .AnyAsync(x => x.Id == id && x.Email == email, cancellationToken);
    }

    public async Task<bool> IsBudgetOwnerAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context
            .Set<BudgetAccess>()
            .AsNoTracking()
            .AnyAsync(x => x.UserId == id && x.IsOwner, cancellationToken);
    }

    public async Task<bool> IsLastActivatedAdministratorAsync(Guid id, CancellationToken cancellationToken)
    {
        var administratorRoleId = await context
            .Set<ApplicationRole>()
            .Where(x => x.Name == ApplicationRoles.Administrator)
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);

        var activatedAdministrators = context
            .Set<ApplicationUser>()
            .Where(x => x.EmailConfirmed)
            .Join(
                context.Set<ApplicationUserRole>(),
                user => user.Id,
                userRole => userRole.UserId,
                (user, userRole) => new
                {
                    User = user,
                    userRole.RoleId
                })
            .Where(x => x.RoleId == administratorRoleId);

        if (!await activatedAdministrators
                .AnyAsync(x => x.User.Id == id, cancellationToken))
        {
            return false;
        }

        return !await activatedAdministrators
            .AnyAsync(x => x.User.Id != id, cancellationToken);
    }

    public async Task<Guid> CreateAsync(string email, UserProfileData data, UserProfilePicture? profilePicture, string password, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = data.FirstName,
            LastName = data.LastName,
            PhoneNumber = data.PhoneNumberE164,
            PreferredCulture = data.PreferredCulture,
            PreferredTheme = data.PreferredTheme,
        };
        var userProfilePicture = profilePicture is null ? null : new ApplicationUserProfilePicture()
        {
            Content = profilePicture.Content,
            ContentType = profilePicture.ContentType,
        };

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await userManager.CreateAsync(user, password);
            result.EnsureSucceeded($"Unable to create user {email}.");

            if (userProfilePicture is not null)
            {
                userProfilePicture.UserId = user.Id;
                await context.AddAsync(userProfilePicture, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            result = await userManager.AddToRolesAsync(user, roles);
            result.EnsureSucceeded($"Unable to affect role(s) {string.Join(", ", roles)} to user {email}.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return user.Id;
    }

    public async Task UpdateAsync(Guid id, UserProfileData data, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var user = await userManager.FindByIdAsync(id.ToString())
                ?? throw new InvalidOperationException($"User {id} does not exist.");

            IdentityResult? result;

            // L'adresse mail n'est jamais mise à jour.

            if (user.PhoneNumber != data.PhoneNumberE164)
            {
                result = await userManager.SetPhoneNumberAsync(user, data.PhoneNumberE164);
                result.EnsureSucceeded($"Unable to update phone number of user {user.Email}.");
            }

            user.FirstName = data.FirstName;
            user.LastName = data.LastName;
            user.PreferredCulture = data.PreferredCulture;
            user.PreferredTheme = data.PreferredTheme;
            await context.SaveChangesAsync(cancellationToken);

            // Update roles
            var currentRoles = await userManager.GetRolesAsync(user);
            var removedRoles = currentRoles.Except(roles, StringComparer.OrdinalIgnoreCase).ToArray();
            var newRoles = roles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
            if (removedRoles.Contains(ApplicationRoles.Administrator, StringComparer.OrdinalIgnoreCase) &&
                await IsLastActivatedAdministratorAsync(id, cancellationToken))
            {
                // Validate rule : "At least one activated user account with role Administrator"
                throw CreateLastActivatedAdministratorException();
            }
            if (newRoles.Length != 0)
            {
                result = await userManager.AddToRolesAsync(user, newRoles);
                result.EnsureSucceeded($"Unable to affect new role(s) {string.Join(", ", newRoles)} to user {user.Email}.");
            }
            if (removedRoles.Length != 0)
            {
                result = await userManager.RemoveFromRolesAsync(user, removedRoles);
                result.EnsureSucceeded($"Unable to remove older role(s) {string.Join(", ", removedRoles)} to user {user.Email}.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var user = await userManager.FindByIdAsync(id.ToString())
                ?? throw new InvalidOperationException($"User {id} does not exist.");

            // Validate rule : "At least one activated user account with role Administrator"
            if (await IsLastActivatedAdministratorAsync(id, cancellationToken))
            {
                throw CreateLastActivatedAdministratorException();
            }

            // Delete budget accesses
            var accesses = context
                .Set<BudgetAccess>()
                .Where(x => x.UserId == id && !x.IsOwner);
            context.RemoveRange(accesses);
            await context.SaveChangesAsync(cancellationToken);

            // Delete user
            var result = await userManager.DeleteAsync(user);
            result.EnsureSucceeded($"Unable to delete user {user.Email}.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ActivateAsync(Guid id, string activationToken, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString())
            ?? throw new InvalidOperationException($"User {id} does not exist.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await userManager.ConfirmEmailAsync(user, activationToken);
            result.EnsureSucceeded($"Unable to confirm email for user {user.Email}.");

            result = await userManager.RemovePasswordAsync(user);
            result.EnsureSucceeded($"Unable to remove temporary password for user {user.Email}.");

            result = await userManager.AddPasswordAsync(user, password);
            result.EnsureSucceeded($"Unable to save user password for user {user.Email}.");

            user.ActivationEmailSentOn = null;
            user.ActivationEmailExpiresOn = null;
            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task SaveActivationEmailAsync(Guid id, DateTimeOffset? sentOn, DateTimeOffset? expiresOn, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString())
            ?? throw new InvalidOperationException($"User {id} does not exist.");

        user.ActivationEmailSentOn = sentOn;
        user.ActivationEmailExpiresOn = expiresOn;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> ChangeEmailAsync(Guid id, string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString())
            ?? throw new InvalidOperationException($"User {id} does not exist.");

        return await userManager.GenerateChangeEmailTokenAsync(user, email);
    }

    public async Task ConfirmEmailChangeAsync(Guid id, string email, string token, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString())
            ?? throw new InvalidOperationException($"User {id} does not exist.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await userManager.ChangeEmailAsync(user, email, token);
            result.EnsureSucceeded($"Unable to confirm email change for user {user.Email}.");

            result = await userManager.SetUserNameAsync(user, email);
            result.EnsureSucceeded($"Unable to change user name for user {email}.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateProfileAsync(Guid id, UserProfileData data, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);

        var user = await userManager.FindByIdAsync(id.ToString())
            ?? throw new InvalidOperationException($"User {id} does not exist.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            IdentityResult? result;

            // L'adresse mail n'est jamais mise à jour.

            if (user.PhoneNumber != data.PhoneNumberE164)
            {
                result = await userManager.SetPhoneNumberAsync(user, data.PhoneNumberE164);
                result.EnsureSucceeded($"Unable to update phone number of user {user.Email}.");
            }

            user.FirstName = data.FirstName;
            user.LastName = data.LastName;
            user.PreferredCulture = data.PreferredCulture;
            user.PreferredTheme = data.PreferredTheme;
            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateUiPreferencesAsync(Guid id, string preferredCulture, string? preferredTheme, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString())
            ?? throw new InvalidOperationException($"User {id} does not exist.");
        
        user.PreferredCulture = preferredCulture;
        user.PreferredTheme = preferredTheme;
        
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateProfilePictureAsync(Guid userId, UserProfilePicture picture, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(picture);

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} does not exist.");
        var profilePicture = await context.FindAsync<ApplicationUserProfilePicture>(user.Id, cancellationToken);

        if (profilePicture is null)
        {
            profilePicture = new ApplicationUserProfilePicture()
            {
                UserId = user.Id,
                Content = picture.Content,
                ContentType = picture.ContentType,
            };
            await context.AddAsync(profilePicture, cancellationToken);
        }
        else
        {
            profilePicture.Content = picture.Content;
            profilePicture.ContentType = picture.ContentType;
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteProfilePictureAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} does not exist.");
        var profilePicture = await context.FindAsync<ApplicationUserProfilePicture>(user.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Profile picture of user {userId} does not exist.");

        context.Remove(profilePicture);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static BadRequestException CreateLastActivatedAdministratorException()
    {
        return new BadRequestException(
            "Request is invalid.",
            [
                new ValidationError(
                    string.Empty,
                    "The last activated administrator cannot be removed.",
                    ErrorCodes.UserIsLastActivatedAdministrator)
            ]);
    }
}
